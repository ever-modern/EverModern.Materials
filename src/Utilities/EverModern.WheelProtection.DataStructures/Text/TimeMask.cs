using System.Collections;

namespace EverModern.WheelProtection.DataStructures.Text;

public class TimeMask(
    TimeOnly minValue,
    TimeOnly maxValue,
    TimeOnly value,
    bool includeSeconds = false
) : IImmutableMask<char, TimeMask>
{
    const char _delimiter = ':';

    readonly IReadOnlyList<char> _chars = includeSeconds
        ? [.. value.ToString("HH:mm:ss")]
        : [.. value.ToString("HH:mm")];

    public char this[int index] => _chars[index];

    public int Count => _chars.Count;

    static (Range HoursRange, Range MinutesRange, Range SecondsRange) GetRanges() =>
        (0..2, 3..5, 6..8);

    public TimeOnly Value => value;

    public TimeMask Change(ContentChange<char> contentChange, out int caretPosition)
    {
        var (at, removed, inserted) = contentChange;
        var length = _chars.Count;
        if (at > length)
            throw new InvalidOperationException("Can't change beyond mask length.");

        char[] slots = [.. _chars];

        var (hoursRange, minutesRange, secondsRange) = GetRanges();

        for (int i = removed - 1; i >= 0; i--)
        {
            var options = GetSlotOptions(slots, at + i);
            slots[at + i] = options[0];
        }

        caretPosition = at;

        if (inserted.Length == 0)
        {
            var newValue = TimeOnly.Parse(slots);
            newValue = Clamp(newValue);

            if (caretPosition > 0 && slots[caretPosition - 1] == _delimiter)
            {
                caretPosition--;
            }

            return new(minValue, maxValue, newValue);
        }

        var insertedCharacters = inserted.Select((c, i) => (c, at + i));

        foreach (var (symbol, insertedAt) in insertedCharacters)
        {
            var currentValue = TimeOnly.Parse(slots);
            var number = symbol - '0';
            bool isNumber = number >= 0 && number <= 9;
            if (hoursRange.Contains(insertedAt) && isNumber)
            {
                var (minDay, maxDay) = AllowedHours();
                var dayMask = new IntegerMask(currentValue.Hour, minDay, maxDay, 2);
                char[] daySlots =
                [
                    .. dayMask.Change(
                        new ContentChange<char>
                        {
                            At = insertedAt - hoursRange.Start.Value,
                            Removed = 0,
                            Inserted = [symbol],
                        },
                        out caretPosition
                    ),
                ];

                daySlots.CopyTo(
                    slots.AsSpan(
                        hoursRange.Start.Value,
                        hoursRange.End.Value - hoursRange.Start.Value
                    )
                );

                caretPosition += hoursRange.Start.Value;
            }
            else if (minutesRange.Contains(insertedAt) && isNumber)
            {
                var (minMinute, maxMinute) = AllowedMinutes(value.Hour);
                var monthMask = new IntegerMask(currentValue.Minute, minMinute, maxMinute, 2);
                char[] minuteSlots =
                [
                    .. monthMask.Change(
                        new ContentChange<char>
                        {
                            At = insertedAt - minutesRange.Start.Value,
                            Removed = 0,
                            Inserted = [symbol],
                        },
                        out caretPosition
                    ),
                ];

                minuteSlots.CopyTo(
                    slots.AsSpan(
                        minutesRange.Start.Value,
                        minutesRange.End.Value - minutesRange.Start.Value
                    )
                );

                var minute = int.Parse(minuteSlots);

                var (minSecond, maxSecond) = AllowedSeconds(currentValue.Hour, minute);

                minute = Math.Clamp(minute, minSecond, maxSecond);

                minute
                    .ToString()
                    .PadLeft(2, '0')
                    .AsSpan()
                    .CopyTo(
                        slots.AsSpan(
                            minutesRange.Start.Value,
                            minutesRange.End.Value - minutesRange.Start.Value
                        )
                    );

                caretPosition += minutesRange.Start.Value;
            }
            else if (secondsRange.Contains(insertedAt) && isNumber)
            {
                if (includeSeconds is false)
                {
                    return this;
                }

                var (minSecond, maxSecond) = AllowedSeconds(currentValue.Hour, currentValue.Minute);

                var secondsMask = new IntegerMask(currentValue.Second, minSecond, maxSecond, 2);

                char[] secondSlots =
                [
                    .. secondsMask.Change(
                        new ContentChange<char>
                        {
                            At = insertedAt - secondsRange.Start.Value,
                            Removed = 0,
                            Inserted = [symbol],
                        },
                        out caretPosition
                    ),
                ];

                caretPosition += secondsRange.Start.Value;

                var secondsValue = int.Parse(secondSlots);

                var (minMinute, maxMinute) = AllowedMinutes(currentValue.Hour);

                var minute = Math.Clamp(currentValue.Minute, minMinute, maxMinute);

                var (minHour, maxHour) = AllowedHours();

                var hour = Math.Clamp(currentValue.Hour, minHour, maxHour);

                hour.ToString()
                    .PadLeft(2, '0')
                    .AsSpan()
                    .CopyTo(
                        slots.AsSpan(
                            hoursRange.Start.Value,
                            hoursRange.End.Value - hoursRange.Start.Value
                        )
                    );

                minute
                    .ToString()
                    .PadLeft(2, '0')
                    .AsSpan()
                    .CopyTo(
                        slots.AsSpan(
                            minutesRange.Start.Value,
                            minutesRange.End.Value - minutesRange.Start.Value
                        )
                    );

                secondsValue
                    .ToString()
                    .PadLeft(2, '0')
                    .AsSpan()
                    .CopyTo(
                        slots.AsSpan(
                            secondsRange.Start.Value,
                            secondsRange.End.Value - secondsRange.Start.Value
                        )
                    );
            }
            else
            {
                if (symbol != _delimiter || IsDelimiterPosition(insertedAt) == false)
                {
                    var newValue = TimeOnly.Parse(slots);
                    return new TimeMask(minValue, maxValue, newValue);
                }
            }
        }

        if (caretPosition < length && slots[caretPosition] == _delimiter)
        {
            caretPosition++;
        }

        var finalValue = TimeOnly.Parse(slots);

        finalValue = Clamp(finalValue);

        return new TimeMask(minValue, maxValue, finalValue, includeSeconds);
    }

    public IEnumerator<char> GetEnumerator() => _chars.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    char[] GetSlotOptions(ReadOnlySpan<char> slots, int position)
    {
        var (hoursRange, minutesRange, secondsRange) = GetRanges();
        if (hoursRange.Contains(position))
        {
            var (minDay, maxDay) = AllowedHours();
            return SlotOptionFunctions.GetOptionsForSlot(
                slotIndex: (byte)(position - hoursRange.Start.Value),
                currentFilling: value.Hour.ToString().PadLeft(2, '0'),
                length: 2,
                min: minDay,
                max: maxDay
            );
        }
        else if (minutesRange.Contains(position))
        {
            var (minMinute, maxMinute) = AllowedMinutes(value.Hour);
            return SlotOptionFunctions.GetOptionsForSlot(
                slotIndex: (byte)(position - minutesRange.Start.Value),
                currentFilling: value.Minute.ToString().PadLeft(2, '0'),
                length: 2,
                min: minMinute,
                max: maxMinute
            );
        }
        else if (secondsRange.Contains(position))
        {
            var (minSecond, maxSecond) = AllowedSeconds(value.Hour, value.Minute);
            return SlotOptionFunctions.GetOptionsForSlot(
                slotIndex: (byte)(position - secondsRange.Start.Value),
                currentFilling: value.Second.ToString().PadLeft(2, '0'),
                length: 2,
                min: minSecond,
                max: maxSecond
            );
        }
        else
        {
            return [':'];
        }
    }

    public (int MinHour, int MaxHour) AllowedHours() => (minValue.Hour, maxValue.Hour);

    public (int MinMonth, int MaxMonth) AllowedMinutes(int hour)
    {
        var (minMinute, maxMinute) = (0, 59);
        if (hour == minValue.Hour)
        {
            minMinute = minValue.Minute;
        }
        if (hour == maxValue.Hour)
        {
            maxMinute = maxValue.Minute;
        }

        return (minMinute, maxMinute);
    }

    public (int MinSecond, int MaxSecond) AllowedSeconds(int hour, int minute)
    {
        var (minSecond, maxSecond) = (0, 59);
        if (hour == minValue.Hour && minute == minValue.Minute)
        {
            minSecond = minValue.Second;
        }

        if (hour == maxValue.Hour && minute == maxValue.Minute)
        {
            maxSecond = maxValue.Second;
        }

        return (minSecond, maxSecond);
    }

    TimeOnly Clamp(TimeOnly valueToClamp) =>
        valueToClamp < minValue ? minValue
        : valueToClamp > maxValue ? maxValue
        : valueToClamp;

    static bool IsDelimiterPosition(int position) =>
        position == 2 || position == 5 || position == 7;
}
