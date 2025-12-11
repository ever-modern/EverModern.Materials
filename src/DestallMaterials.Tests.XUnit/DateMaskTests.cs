using DestallMaterials.WheelProtection.DataStructures.Text;

namespace DestallMaterials.Tests.XUnit;

public class DateMaskTests
{
    static DateMask Create(string input) =>
        new(new(DateFormat.DayMonthYear), new(1975, 1, 1), new(2025, 1, 1), DateOnly.Parse(input));

    [Fact]
    public void WriteAtSeparator()
    {
        var mask = Create("22.12.2020");

        var result = mask.Change(new(8, 0, ['1']), out var caretPosition);

        Assert.Equal(9, caretPosition);
        Assert.Equal(result, [.. "22.12.2010"]);
    }
}
