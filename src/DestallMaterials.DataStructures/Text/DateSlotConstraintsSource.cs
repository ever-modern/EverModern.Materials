using DestallMaterials.WheelProtection.DataStructures.Time;

namespace DestallMaterials.WheelProtection.DataStructures.Text;

// DateSlotConstraintsSource computes constraints for an8-slot date layout: ddMMyyyy
public class DateSlotConstraintsSource : ISlotConstraintsSource<char>
{
    readonly DateTimeRange _range;
    readonly int _yearDigits;
    public int Length => 4 + _yearDigits;

    public DateSlotConstraintsSource(DateTimeRange range, byte yearDigits = 4)
    {
        _range = range;
        _yearDigits = Math.Clamp((int)yearDigits, 1, 10);
    }

    public (int[] Days, int[] Months, int[] Years) GetValueConstraints()
    {
        // Compute full set of years, months and days available within the provided range
        var years = Enumerable
            .Range(_range.Start.Year, _range.End.Year - _range.Start.Year + 1)
            .ToArray();

        // Months that appear for at least one year in range
        var months = Enumerable
            .Range(1, 12)
            .Where(m =>
                years.Any(y =>
                {
                    try
                    {
                        var start = new DateTime(y, m, 1);
                        var end = new DateTime(y, m, DateTime.DaysInMonth(y, m));
                        return !(end < _range.Start || start > _range.End);
                    }
                    catch
                    {
                        return false;
                    }
                })
            )
            .ToArray();

        // Days: union of possible day numbers for all month-year combos inside range
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
        {
            // fallback to 1..31
            for (int d = 1; d <= 31; d++)
                daysSet.Add(d);
        }

        var days = daysSet.OrderBy(d => d).ToArray();

        return (days, months, years);
    }

    public IReadOnlyList<SlotConstraint<char>> GetConstraints(IReadOnlyList<char?> currentFilling)
    {
        // Use precomputed value constraints as a base
        var (allDays, allMonths, allYears) = GetValueConstraints();

        // slots: d1,d2,m1,m2, y1..yN
        var totalSlots = 4 + _yearDigits;

        var slots = Enumerable
            .Range(0, totalSlots)
            .Select(i => i < currentFilling.Count ? currentFilling[i] : default(char?))
            .ToArray();

        // Extract year slot slice
        char?[] yearSlots = new char?[_yearDigits];
        for (int i = 0; i < _yearDigits; i++)
            yearSlots[i] = slots[4 + i];

        // Filter candidate years that match partial year slots and are within overall years
        var candidateYears = allYears.Where(y => YearMatchesSlots(y, yearSlots)).ToArray();

        // Determine candidate months matching month slots and within months available in range
        var monthSlots = (slots[2], slots[3]);

        var candidateMonths = allMonths
            .Where(m => MonthMatchesSlots(m, monthSlots))
            .Where(m => candidateYears.Any(y => MonthYearInRange(y, m)))
            .ToArray();

        // Compute set of possible max days for valid month-year combos within filtered candidates
        var possibleMaxDays = new HashSet<int>();
        foreach (var y in candidateYears)
        {
            foreach (var m in candidateMonths)
            {
                if (!MonthYearInRange(y, m))
                    continue;
                try
                {
                    possibleMaxDays.Add(DateTime.DaysInMonth(y, m));
                }
                catch { }
            }
        }

        if (possibleMaxDays.Count == 0)
        {
            // fallback: use allDays to derive possible maximums
            foreach (var y in allYears)
            foreach (var m in allMonths)
                try
                {
                    possibleMaxDays.Add(DateTime.DaysInMonth(y, m));
                }
                catch { }
        }

        if (possibleMaxDays.Count == 0)
            possibleMaxDays.Add(31);

        // Day tens options
        var d1Options = new List<char?>();
        for (int d1 = 0; d1 <= 3; d1++)
        {
            bool ok = false;
            for (int d2 = 0; d2 <= 9 && !ok; d2++)
            {
                int day = d1 * 10 + d2;
                if (day >= 1 && possibleMaxDays.Any(max => day <= max))
                    ok = true;
            }
            if (ok)
                d1Options.Add((char)('0' + d1));
        }

        // Day units
        var d2Options = new List<char?>();
        var firstSlot = slots[0];
        if (firstSlot is not null)
        {
            int d1 = firstSlot.Value - '0';
            for (int d2 = 0; d2 <= 9; d2++)
            {
                int day = d1 * 10 + d2;
                if (day >= 1 && possibleMaxDays.Any(max => day <= max))
                    d2Options.Add((char)('0' + d2));
            }
        }
        else
        {
            for (int d2 = 0; d2 <= 9; d2++)
            {
                bool ok = false;
                for (int d1 = 0; d1 <= 3 && !ok; d1++)
                {
                    int day = d1 * 10 + d2;
                    if (day >= 1 && possibleMaxDays.Any(max => day <= max))
                        ok = true;
                }
                if (ok)
                    d2Options.Add((char)('0' + d2));
            }
        }

        // Month digit options based on candidateMonths
        var m1Options = new HashSet<char?>();
        var m2Options = new HashSet<char?>();
        foreach (var m in candidateMonths)
        {
            var s = m.ToString("D2");
            m1Options.Add(s[0]);
            m2Options.Add(s[1]);
        }

        // Year digits options: for each digit position, gather digits present in candidateYears last N digits
        var yearDigitOptions = new List<char?[]>();
        var candidateYearStrings = candidateYears
            .Select(y => FormatYearForDigits(y, _yearDigits))
            .ToArray();
        for (int pos = 0; pos < _yearDigits; pos++)
        {
            var set = new HashSet<char?>();
            foreach (var ys in candidateYearStrings)
            {
                if (ys.Length == _yearDigits)
                    set.Add(ys[pos]);
            }
            // if nothing matched (e.g., candidateYears empty) allow0..9 for this digit
            if (set.Count == 0)
                set.UnionWith(Enumerable.Range(0, 10).Select(d => (char?)('0' + d)));

            yearDigitOptions.Add(set.ToArray());
        }

        // Build final constraints list
        List<SlotConstraint<char>> constraints =
        [
            new SlotConstraint<char>([.. d1Options]),
            new SlotConstraint<char>([.. d2Options]),
            new SlotConstraint<char>([.. m1Options]),
            new SlotConstraint<char>([.. m2Options]),
        ];

        foreach (var arr in yearDigitOptions)
            constraints.Add(new SlotConstraint<char>(arr));

        return constraints;
    }

    bool MonthYearInRange(int year, int month)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        return !(monthEnd < _range.Start || monthStart > _range.End);
    }

    static bool MonthMatchesSlots(int month, (char? m1, char? m2) monthSlots)
    {
        var s = month.ToString("D2");
        if (monthSlots.m1 is not null && monthSlots.m1 != s[0])
            return false;
        if (monthSlots.m2 is not null && monthSlots.m2 != s[1])
            return false;
        return true;
    }

    static bool YearMatchesSlots(int year, char?[] yearSlots)
    {
        var s = FormatYearForDigits(year, yearSlots.Length);
        if (s.Length != yearSlots.Length)
            return false;
        for (int i = 0; i < yearSlots.Length; i++)
        {
            if (yearSlots[i] is not null && yearSlots[i]!.Value != s[i])
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
}
