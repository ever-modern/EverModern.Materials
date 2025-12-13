using DestallMaterials.WheelProtection.DataStructures.Text;

namespace DestallMaterials.Tests.XUnit;

public class DateMaskTests
{
    static DateMask Create(string input) =>
        new(new(DateFormat.DayMonthYear), new(1975, 1, 1), new(2025, 1, 1), DateOnly.Parse(input));

    [Fact]
    public void WriteADigitToYear_Simple()
    {
        var mask = Create("22.12.2020");

        var result = mask.Change(new(8, 0, ['1']), out var caretPosition);

        Assert.Equal(9, caretPosition);
        Assert.Equal(result, [.. "22.12.2010"]);
    }

    [Fact]
    public void EraseDelimiter()
    {
        var mask = Create("22.12.2020");

        var result = mask.Change(new(5, 1, []), out var caretPosition);

        Assert.Equal(5, caretPosition);
        Assert.Equal(result, [.. "22.12.2020"]);
    }

    [Fact]
    public void EraseFirstMonthDigit()
    {
        const string input = "23.04.2000";
        var mask = Create(input);

        var result = mask.Change(new(3, 1, []), out var caretPosition);

        Assert.Equal(2, caretPosition);
        Assert.Equal(result, [.. input]);
    }
}
