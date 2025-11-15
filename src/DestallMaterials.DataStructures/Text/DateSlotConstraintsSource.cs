using DestallMaterials.WheelProtection.DataStructures.Time;
using static DestallMaterials.WheelProtection.DataStructures.Text.SlotOptionFunctions;

namespace DestallMaterials.WheelProtection.DataStructures.Text;

public class DateSlotConstraintsSource(DateTimeRange range, DateFormatting formatting)
    : ISlotConstraintsSource<char>
{
    public int Length => formatting.Length;

    public SlotConstraint<char> GetSlotConstraints(
        int slotIndex,
        IReadOnlyList<char> currentFilling
    )
    {
        if (currentFilling.Count != formatting.Length)
        {
            throw new InvalidOperationException(
                "Current filling doesn't match the date format length."
            );
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(slotIndex, Length, nameof(slotIndex));
        ArgumentOutOfRangeException.ThrowIfNegative(slotIndex, nameof(slotIndex));

        Span<char> slots = [.. currentFilling];
        var (dateFormat, delimiter, yearLength, dayChar, monthChar, yearChar) = formatting;

        var (minDate, maxDate) = range;

        var (dayRange, monthRange, yearRange) = formatting.GetComponentRanges();

        int.TryParse(new string(slots[yearRange]), out var year);
        int.TryParse(new string(slots[monthRange]), out var month);

        if (Contains(dayRange, slotIndex))
        {
            var (min, max) = (1, MaxDay(slots[monthRange], slots[yearRange]));

            if (year == minDate.Year && month == minDate.Month)
            {
                min = minDate.Day;
            }
            if (year == maxDate.Year && month == maxDate.Month)
            {
                max = maxDate.Day;
            }

            var result = GetOptionsForSlot(
                slotIndex: slotIndex - dayRange.Start.Value,
                min: min,
                max: max,
                length: 2,
                currentFilling: slots[dayRange]
            );

            return result;
        }
        else if (Contains(monthRange, slotIndex))
        {
            var (min, max) = (1, 12);
            if (year == minDate.Year)
            {
                min = minDate.Month;
            }
            if (year == maxDate.Year)
            {
                max = maxDate.Month;
            }

            var result = GetOptionsForSlot(
                slotIndex: slotIndex - monthRange.Start.Value,
                min: min,
                max: max,
                length: 2,
                currentFilling: slots[monthRange]
            );

            return result;
        }
        else if (Contains(yearRange, slotIndex))
        {
            var minDateYear = minDate.Year;
            var maxDateYear = maxDate.Year;
            var result = GetOptionsForSlot(
                slotIndex: slotIndex - yearRange.Start.Value,
                min: minDateYear,
                max: maxDateYear,
                length: yearLength,
                currentFilling: slots[yearRange]
            );

            return result;
        }

        return new([formatting.Delimiter]);
    }

    int MaxDay(Span<char> monthDigits, Span<char> yearDigits)
    {
        var (mFirst, mSecond) = (monthDigits[0], monthDigits[1]);

        if (char.IsDigit(mSecond) == false)
        {
            return 31;
        }

        var result = mSecond switch
        {
            '3' or '5' or '7' or '8' or '0' => 31,
            '4' or '6' or '9' => 30,
            '1' => mFirst switch
            {
                '0' => 31,
                '1' => 30,
                _ => 31,
            },
            '2' => char.IsDigit(yearDigits[^1]) == false
                ? 31
                : mFirst switch
                {
                    '0' => yearDigits.Length > 1
                    && char.IsDigit(yearDigits[^2])
                    && (
                        SlotOptionFunctions.ToNumber(yearDigits[^2]) * 10
                        + SlotOptionFunctions.ToNumber(yearDigits[^1])
                    ) % 4
                        == 0
                        ? 29
                        : 28,
                    _ => 31,
                },
            _ => 31,
        };

        return result;
    }

    static bool Contains(Range range, int number) =>
        number >= range.Start.Value && number < range.End.Value;
}

public enum DateFormat
{
    DayMonthYear,
    MonthDayYear,
    YearMonthDay,
    MonthDay,
    DayMonth,
}

public record DateFormatting(
    DateFormat DateFormat = DateFormat.DayMonthYear,
    char Delimiter = '.',
    byte YearLength = 4,
    char DayChar = 'd',
    char MonthChar = 'M',
    char YearChar = 'y'
)
{
    public int Length =>
        DateFormat switch
        {
            DateFormat.DayMonthYear => 2 + 1 + 2 + 1 + YearLength,
            DateFormat.MonthDayYear => 2 + 1 + 2 + 1 + YearLength,
            DateFormat.YearMonthDay => YearLength + 1 + 2 + 1 + 2,
            DateFormat.MonthDay => 2 + 1 + 2,
            DateFormat.DayMonth => 2 + 1 + 2,
            _ => throw new InvalidOperationException("Unknown date format."),
        };

    public static DateFormatting Parse(string format)
    {
        // very small parser for e.g. "dd.MM.yyyy"
        if (string.IsNullOrEmpty(format))
            throw new ArgumentNullException(nameof(format));
        // detect delimiter as the first non alpha char
        char? delim = null;
        foreach (var c in format)
        {
            if (!char.IsLetter(c))
            {
                delim = c;
                break;
            }
        }
        if (!delim.HasValue)
            throw new InvalidOperationException("Unsupported format");

        var parts = format.Split(delim.Value);
        if (parts.Length == 3)
        {
            var p0 = parts[0];
            var p1 = parts[1];
            var p2 = parts[2];
            if (p0.Length == 2 && p1.Length == 2 && p2.Length >= 2)
            {
                // assume day-month-year ordering if format starts with 'd'
                if (char.ToLowerInvariant(p0[0]) == 'd')
                    return new DateFormatting(
                        DateFormat.DayMonthYear,
                        delim.Value,
                        (byte)p2.Length
                    );

                if (char.ToLowerInvariant(p0[0]) == 'm')
                    return new DateFormatting(
                        DateFormat.MonthDayYear,
                        delim.Value,
                        (byte)p2.Length
                    );

                if (char.ToLowerInvariant(p0[0]) == 'y')
                    return new DateFormatting(
                        DateFormat.YearMonthDay,
                        delim.Value,
                        (byte)p0.Length
                    );
            }
        }

        throw new InvalidOperationException("Unsupported format");
    }

    public override string ToString()
    {
        // Build format like "dd.MM.yyyy" based on the formatting fields
        static string Repeat(char c, int count) => new string(c, count);

        var d = DayChar;
        var m = MonthChar;
        var y = YearChar;
        var s = Delimiter;

        var result = DateFormat switch
        {
            DateFormat.DayMonthYear => Repeat(DayChar, 2)
                + Delimiter
                + Repeat(MonthChar, 2)
                + Delimiter
                + Repeat(YearChar, YearLength),
            DateFormat.MonthDayYear => Repeat(MonthChar, 2)
                + Delimiter
                + Repeat(DayChar, 2)
                + Delimiter
                + Repeat(YearChar, YearLength),
            DateFormat.YearMonthDay => Repeat(YearChar, YearLength)
                + Delimiter
                + Repeat(MonthChar, 2)
                + Delimiter
                + Repeat(DayChar, 2),
            DateFormat.MonthDay => Repeat(MonthChar, 2) + Delimiter + Repeat(DayChar, 2),
            DateFormat.DayMonth => Repeat(DayChar, 2) + Delimiter + Repeat(MonthChar, 2),
            _ => base.ToString(),
        };

        return result!;
    }

    public (char[] Year, char[] Month, char[] Day) BreakIntoComponents(char[] symbols)
    {
        var yearLength = YearLength;
        var (year, month, day) = GetComponentRanges();
        return (symbols[year].ToArray(), symbols[month].ToArray(), symbols[day].ToArray());
    }

    public (Range Year, Range Month, Range Day) GetComponentRanges()
    {
        var yearLength = YearLength;
        var ranges = DateFormat switch
        {
            DateFormat.DayMonthYear => (0..2, 3..5, 6..(6 + yearLength)),
            DateFormat.MonthDayYear => (3..5, 0..2, 6..(6 + yearLength)),
            DateFormat.YearMonthDay => (
                (1 + yearLength)..(3 + yearLength),
                (3 + yearLength + 1)..(5 + yearLength),
                0..yearLength
            ),
            DateFormat.MonthDay => (3..5, 0..2, 0..0),
            DateFormat.DayMonth => (0..2, 3..5, 0..0),
            _ => throw new InvalidOperationException("Unknown date format."),
        };
        return ranges;
    }

    public string Stringify(DateOnly dateOnly) => dateOnly.ToString(ToString());

    public string Concat(IEnumerable<char> day, IEnumerable<char> month, IEnumerable<char> year) =>
        DateFormat switch
        {
            DateFormat.DayMonthYear => string.Concat(day)
                + Delimiter
                + string.Concat(month)
                + Delimiter
                + string.Concat(year),
            DateFormat.MonthDayYear => string.Concat(month)
                + Delimiter
                + string.Concat(day)
                + Delimiter
                + string.Concat(year),
            DateFormat.YearMonthDay => string.Concat(year)
                + Delimiter
                + string.Concat(month)
                + Delimiter
                + string.Concat(day),
            DateFormat.MonthDay => string.Concat(month) + Delimiter + string.Concat(day),
            DateFormat.DayMonth => string.Concat(day) + Delimiter + string.Concat(month),
            _ => throw new InvalidOperationException("Unknown date format."),
        };
}
