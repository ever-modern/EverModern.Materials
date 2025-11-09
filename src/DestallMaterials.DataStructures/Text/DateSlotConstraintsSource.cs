using DestallMaterials.WheelProtection.DataStructures.Time;
using Microsoft.VisualBasic;

namespace DestallMaterials.WheelProtection.DataStructures.Text;

public class DateSlotConstraintsSource(DateTimeRange range, DateFormatting formatting)
    : ISlotConstraintsSource<char>
{
    static readonly char[] _numberChars = [.. Enumerable.Range((int)'0', 10).Select(i => (char)i)];

    public int Length => formatting.Length;

    public (int[] Days, int[] Months, int[] Years) GetValueConstraints()
    {
        var years = Enumerable
            .Range(range.Start.Year, range.End.Year - range.Start.Year + 1)
            .ToArray();

        var months = Enumerable
            .Range(1, 12)
            .Where(m =>
                years.Any(y =>
                {
                    try
                    {
                        var start = new DateTime(y, m, 1);
                        var end = new DateTime(y, m, DateTime.DaysInMonth(y, m));
                        return !(end < range.Start || start > range.End);
                    }
                    catch
                    {
                        return false;
                    }
                })
            )
            .ToArray();

        var daysSet = new HashSet<int>();
        foreach (var y in years)
        {
            foreach (var m in months)
            {
                if (!MonthYearInRange(y, m))
                    continue;
                try
                {
                    var dmax = DateTime.DaysInMonth(y, m);
                    for (int d = 1; d <= dmax; d++)
                        daysSet.Add(d);
                }
                catch { }
            }
        }

        if (daysSet.Count == 0)
            for (int d = 1; d <= 31; d++)
                daysSet.Add(d);

        return (daysSet.OrderBy(x => x).ToArray(), months, years);
    }

    bool MonthYearInRange(int y, int m)
    {
        var monthStart = new DateTime(y, m, 1);
        var monthEnd = new DateTime(y, m, DateTime.DaysInMonth(y, m));
        return !(monthEnd < range.Start || monthStart > range.End);
    }

    bool MonthMatchesSlots(int month, char? m1, char? m2)
    {
        var s = month.ToString("D2");
        // treat null, default(char) or placeholder MonthChar as unspecified (wildcard)
        if (
            m1.HasValue
            && m1.Value != default(char)
            && m1.Value != formatting.MonthChar
            && m1.Value != s[0]
        )
            return false;
        if (
            m2.HasValue
            && m2.Value != default(char)
            && m2.Value != formatting.MonthChar
            && m2.Value != s[1]
        )
            return false;
        return true;
    }

    bool YearMatchesSlots(int year, char?[] yearSlots)
    {
        var s = FormatYearForDigits(year, yearSlots.Length);
        if (s.Length != yearSlots.Length)
            return false;
        for (int i = 0; i < yearSlots.Length; i++)
        {
            // treat null, default(char) or placeholder YearChar as unspecified (wildcard)
            if (
                yearSlots[i].HasValue
                && yearSlots[i].Value != default(char)
                && yearSlots[i].Value != formatting.YearChar
                && yearSlots[i].Value != s[i]
            )
                return false;
        }
        return true;
    }

    static string FormatYearForDigits(int year, int digits)
    {
        var str = year.ToString();
        if (str.Length >= digits)
            return str.Substring(str.Length - digits);
        return str.PadLeft(digits, '0');
    }

    public IReadOnlyList<SlotConstraint<char>> GetConstraints(IReadOnlyList<char> currentFilling)
    {
        if (currentFilling.Count != formatting.Length)
        {
            throw new InvalidOperationException(
                "Current filling doesn't match the date format length."
            );
        }

        var slots = currentFilling.ToArray().AsSpan();
        var (dateFormat, delimiter, yearLength, dayChar, monthChar, yearChar) = formatting;

        var (minDate, maxDate) = range;

        var (dayRange, monthRange, yearRange) = dateFormat switch
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

        int.TryParse(new string(slots[yearRange]), out var year);
        int.TryParse(new string(slots[monthRange]), out var month);

        var monthOptions = 1..12;
        var dayOptions = 1..MaxDay(slots[monthRange], slots[yearRange]);
        if (year == minDate.Year)
        {
            monthOptions = (minDate.Month..12);

            if (month == minDate.Month)
            {
                dayOptions = (minDate.Day..dayOptions.End.Value);
            }
        }
        if (year == maxDate.Year)
        {
            monthOptions = (monthOptions.Start.Value..maxDate.Month);

            if (month == maxDate.Month)
            {
                dayOptions = (dayOptions.Start.Value..maxDate.Day);
            }
        }

        var minDateYear = minDate.Year;
        var maxDateYear = maxDate.Year;
        var yearOptions = ToCharOptions(minDateYear, maxDateYear, yearLength, slots[yearRange]);

        SlotConstraint<char>[] constraints = new SlotConstraint<char>[formatting.Length];

        var monthOptionsResult = ToCharOptions(
            monthOptions.Start.Value,
            monthOptions.End.Value,
            2,
            slots[monthRange]
        );
        var dayOptionsResult = ToCharOptions(
            dayOptions.Start.Value,
            dayOptions.End.Value,
            2,
            slots[dayRange]
        );

        yearOptions
            .Select(o => new SlotConstraint<char>(o))
            .ToArray()
            .CopyTo(constraints, yearRange.Start.Value);
        monthOptionsResult
            .Select(o => new SlotConstraint<char>(o))
            .ToArray()
            .CopyTo(constraints, monthRange.Start.Value);
        dayOptionsResult
            .Select(o => new SlotConstraint<char>(o))
            .ToArray()
            .CopyTo(constraints, dayRange.Start.Value);

        return [.. constraints.Select(c => c.Options is not null ? c : new([delimiter]))];
    }

    static char[][] ToCharOptions(
        int minValue,
        int maxValue,
        byte length,
        Span<char> currentFilling
    )
    {
        char[][] result = new char[length][];

        var prevDiff = 0;
        bool atTop = true;
        for (int i = length - 1; i >= 0; i--)
        {
            var divider = (int)Math.Pow(10, i);
            var from = (minValue / divider) % 10;
            var to = (maxValue / divider) % 10;

            if (prevDiff > 1 || prevDiff == 1 && from < to)
            {
                from = 0;
                to = 9;
            }

            var at = length - 1 - i;

            if (from <= to)
            {
                result[at] = [.. Enumerable.Range(from, (to - from + 1)).Select(ToChar)];
            }
            else
            {
                result[at] =
                [
                    .. Enumerable.Range(to, 10 - to).Select(ToChar),
                    .. Enumerable.Range(0, from).Select(ToChar),
                ];
            }

            var currentChar = currentFilling[at];
            if (char.IsDigit(currentChar))
            {
                var current = ToNumber(currentChar);
                prevDiff = current == to ? 0 : 2;
            }
            else
            {
                prevDiff = to - from >= 0 ? to - from : 2;
            }
        }

        return result;
    }

    static readonly Dictionary<
        (DateTimeRange, DateFormatting),
        Func<IReadOnlyList<char>, char, int, bool>
    > _digitValidators = [];

    static T[] WithAt<T>(IReadOnlyList<T> source, int position, T item)
    {
        T[] result = [.. source];
        result[position] = item;
        return result;
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
                    && (ToNumber(yearDigits[^2]) * 10 + ToNumber(yearDigits[^1])) % 4 == 0
                        ? 29
                        : 28,
                    _ => 31,
                },
            _ => 31,
        };

        return result;
    }

    static byte ToNumber(char digit) => (byte)(digit - '0');

    static char ToChar(int digit) => (char)((digit % 10) + '0');
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
}
