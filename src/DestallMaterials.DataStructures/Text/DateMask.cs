using System.Collections;

namespace DestallMaterials.WheelProtection.DataStructures.Text;

public class DateMask(DateFormatting format, DateOnly minValue, DateOnly maxValue, DateOnly value)
    : IImmutableMask<char, DateMask>
{
    readonly string _formedDate = format.Stringify(value);

    public char this[int index] => _formedDate[index];

    public int Count => format.Length;

    public DateOnly Value => value;

    public DateMask Change(ContentChange<char> contentChange, out int caretPosition)
    {
        var (at, removed, inserted) = contentChange;
        var length = _formedDate.Length;
        if (at > length)
            throw new InvalidOperationException("Can't change beyond mask length.");

        char[] slots = [.. _formedDate];

        var (dayRange, monthRange, yearRange) = format.GetComponentRanges();

        for (int i = removed - 1; i >= 0; i--)
        {
            var options = GetSlotOptions(slots, at + i);
            slots[at + i] = options[0];
        }

        caretPosition = at;

        if (inserted.Length == 0)
        {
            var newValue = DateOnly.Parse(slots);
            newValue = Clamp(newValue);

            if (caretPosition > 0 && slots[caretPosition - 1] == format.Delimiter)
            {
                caretPosition--;
            }

            return new(format, minValue, maxValue, newValue);
        }

        var insertedCharacters = inserted.Select((c, i) => (c, at + i));

        foreach (var (symbol, insertedAt) in insertedCharacters)
        {
            var currentValue = DateOnly.Parse(slots);
            var number = symbol - '0';
            bool isNumber = number >= 0 && number <= 9;
            if (dayRange.Contains(insertedAt) && isNumber)
            {
                var (minDay, maxDay) = AllowedDays(currentValue);
                var dayMask = new IntegerMask(currentValue.Day, minDay, maxDay, 2);
                char[] daySlots =
                [
                    .. dayMask.Change(
                        new ContentChange<char>
                        {
                            At = insertedAt - dayRange.Start.Value,
                            Removed = 0,
                            Inserted = [symbol],
                        },
                        out caretPosition
                    ),
                ];

                daySlots.CopyTo(
                    slots.AsSpan(dayRange.Start.Value, dayRange.End.Value - dayRange.Start.Value)
                );

                caretPosition += dayRange.Start.Value;
            }
            else if (monthRange.Contains(insertedAt) && isNumber)
            {
                var (minMonth, maxMonth) = AllowedMonths(currentValue);
                var monthMask = new IntegerMask(currentValue.Month, minMonth, maxMonth, 2);
                char[] monthSlots =
                [
                    .. monthMask.Change(
                        new ContentChange<char>
                        {
                            At = insertedAt - monthRange.Start.Value,
                            Removed = 0,
                            Inserted = [symbol],
                        },
                        out caretPosition
                    ),
                ];

                monthSlots.CopyTo(
                    slots.AsSpan(
                        monthRange.Start.Value,
                        monthRange.End.Value - monthRange.Start.Value
                    )
                );

                var day = currentValue.Day;

                var (minDay, maxDay) = AllowedDays(currentValue.Year, int.Parse(monthSlots));

                day = Math.Clamp(day, minDay, maxDay);

                day.ToString()
                    .PadLeft(2, '0')
                    .AsSpan()
                    .CopyTo(
                        slots.AsSpan(
                            dayRange.Start.Value,
                            dayRange.End.Value - dayRange.Start.Value
                        )
                    );

                caretPosition += monthRange.Start.Value;
            }
            else if (yearRange.Contains(insertedAt) && isNumber)
            {
                var minYear = minValue.Year;
                var maxYear = maxValue.Year;

                var yearMask = new IntegerMask(
                    currentValue.Year,
                    minYear,
                    maxYear,
                    format.YearLength
                );

                char[] yearSlots =
                [
                    .. yearMask.Change(
                        new ContentChange<char>
                        {
                            At = insertedAt - yearRange.Start.Value,
                            Removed = 0,
                            Inserted = [symbol],
                        },
                        out caretPosition
                    ),
                ];

                caretPosition += yearRange.Start.Value;

                var yearValue = int.Parse(yearSlots);

                var (minMonth, maxMonth) = AllowedMonths(yearValue);

                var monthValue = Math.Clamp(currentValue.Month, minMonth, maxMonth);

                var (minDay, maxDay) = AllowedDays(yearValue, monthValue);

                var day = Math.Clamp(currentValue.Day, minDay, maxDay);

                day.ToString()
                    .PadLeft(2, '0')
                    .AsSpan()
                    .CopyTo(
                        slots.AsSpan(
                            dayRange.Start.Value,
                            dayRange.End.Value - dayRange.Start.Value
                        )
                    );

                monthValue
                    .ToString()
                    .PadLeft(2, '0')
                    .AsSpan()
                    .CopyTo(
                        slots.AsSpan(
                            monthRange.Start.Value,
                            monthRange.End.Value - monthRange.Start.Value
                        )
                    );

                yearValue
                    .ToString()
                    .PadLeft(format.YearLength, '0')
                    .AsSpan()
                    .CopyTo(
                        slots.AsSpan(
                            yearRange.Start.Value,
                            yearRange.End.Value - yearRange.Start.Value
                        )
                    );
            }
            else
            {
                if (symbol != format.Delimiter || IsDelimiterPosition(insertedAt) == false)
                {
                    var newValue = DateOnly.Parse(slots);
                    return new DateMask(format, minValue, maxValue, newValue);
                }
            }

            //caretPosition = insertedAt + 1;
        }

        if (caretPosition < format.Length && slots[caretPosition] == format.Delimiter)
        {
            caretPosition++;
        }

        var finalValue = DateOnly.Parse(slots);

        finalValue = Clamp(finalValue);

        return new DateMask(format, minValue, maxValue, finalValue);
    }

    public IEnumerator<char> GetEnumerator() => _formedDate.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    enum MaskPosition
    {
        Delimiter,
        Day,
        Month,
        Year,
    }

    (int MinMonth, int MaxMonth) AllowedMonths(DateOnly dateOnly) => AllowedMonths(dateOnly.Year);

    (int MinMonth, int MaxMonth) AllowedMonths(int year)
    {
        var (minMonth, maxMonth) = (1, 12);
        if (year == minValue.Year)
        {
            minMonth = minValue.Month;
        }
        if (year == maxValue.Year)
        {
            maxMonth = maxValue.Month;
        }

        return (minMonth, maxMonth);
    }

    (int MinDay, int MaxDay) AllowedDays(int year, int month)
    {
        var maxDay = DateTime.DaysInMonth(year, month);
        var minDay = 1;

        if (year == minValue.Year && month == minValue.Month)
        {
            minDay = minValue.Day;
        }
        if (year == maxValue.Year && month == minValue.Month)
        {
            maxDay = maxValue.Day;
        }

        return (minDay, maxDay);
    }

    (int MinDay, int MaxDay) AllowedDays(DateOnly date) => AllowedDays(date.Year, date.Month);

    char[] GetSlotOptions(ReadOnlySpan<char> slots, int position)
    {
        var date = DateOnly.Parse(slots);
        var (dayRange, monthRange, yearRange) = format.GetComponentRanges();
        if (dayRange.Contains(position))
        {
            var (minDay, maxDay) = AllowedDays(value);
            return SlotOptionFunctions.GetOptionsForSlot(
                slotIndex: (byte)(position - dayRange.Start.Value),
                currentFilling: value.Day.ToString().PadLeft(2, '0'),
                length: 2,
                min: minDay,
                max: maxDay
            );
        }
        else if (monthRange.Contains(position))
        {
            var (minMonth, maxMonth) = AllowedMonths(value);
            return SlotOptionFunctions.GetOptionsForSlot(
                slotIndex: (byte)(position - monthRange.Start.Value),
                currentFilling: value.Month.ToString().PadLeft(2, '0'),
                length: 2,
                min: minMonth,
                max: maxMonth
            );
        }
        else if (yearRange.Contains(position))
        {
            return SlotOptionFunctions.GetOptionsForSlot(
                slotIndex: (byte)(position - yearRange.Start.Value),
                currentFilling: value.Year.ToString().PadLeft(format.YearLength, '0'),
                length: format.YearLength,
                min: minValue.Year,
                max: maxValue.Year
            );
        }
        else
        {
            return [format.Delimiter];
        }
    }

    DateOnly Clamp(DateOnly value)
    {
        if (value > maxValue)
        {
            return maxValue;
        }

        if (value < minValue)
        {
            return minValue;
        }

        return value;
    }

    bool IsDelimiterPosition(int position)
    {
        var (yearRange, monthRange, dayRange) = format.GetComponentRanges();
        return !dayRange.Contains(position)
            && !monthRange.Contains(position)
            && !yearRange.Contains(position);
    }
}
