using DestallMaterials.WheelProtection.DataStructures.Text;

namespace DestallMaterials.Tests.XUnit;

public class NumberMaskTests
{
    [Fact]
    public void WriteOverflowingValue()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);

        var mask = new Mask<char>(numberConstraints, initialSlots: [.. from.ToString()]);

        var carretPosition = mask.AcceptChange(new(At: 0, Removed: 1, Inserted: ['2']));

        Assert.Equal(1, carretPosition);
        Assert.Equal([.. "2000"], mask.Slots);
    }

    [Fact]
    public void WriteOverflowingValue_MustWriteToEnd()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);

        var mask = new Mask<char>(numberConstraints, initialSlots: [.. from.ToString()]);

        var carretPosition = mask.AcceptChange(new(At: 0, Removed: 1, Inserted: ['8']));

        Assert.Equal(3, carretPosition);
        Assert.Equal([.. "1985"], mask.Slots);
    }

    [Fact]
    public void WriteTopEdgeNumber()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);

        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "2020"]);

        var carretPosition = mask.AcceptChange(new(At: 3, Removed: 1, Inserted: ['5']));

        Assert.Equal(4, carretPosition);
        Assert.Equal([.. "2025"], mask.Slots);
    }
}
