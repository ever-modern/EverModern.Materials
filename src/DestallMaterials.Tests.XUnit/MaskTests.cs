using DestallMaterials.WheelProtection.DataStructures.Text;

namespace DestallMaterials.Tests.XUnit;

using Digit = byte;

class SimpleTextConstraintsSpurce : ISlotConstraintsSource<Digit>
{
    static readonly SlotConstraint<Digit>[] _result =
    [
        .. Enumerable
            .Range(1, 4)
            .Select(i => new SlotConstraint<Digit>(
                Options: [.. Enumerable.Range(0, 10).Select(i => (Digit?)i)]
            )),
    ];

    public IReadOnlyList<SlotConstraint<Digit>> GetConstraints(IReadOnlyList<Digit?> currentFilling) =>
        _result;
}

public class MaskTests
{
    readonly Mask<Digit> Mask;

    public MaskTests()
    {
        Mask = new(
            constraintsSource: new SimpleTextConstraintsSpurce(),
            initialSlots: [0, 0, 0, 0],
            equalityComparer: EqualityComparer<Digit?>.Default
        );
    }

    [Fact]
    public void InitMask()
    {
        var init = Mask.Slots;

        Assert.Equal(4, init.Count);
        Assert.Equal([0, 0, 0, 0], init);
    }

    [Fact]
    public void EraseRight()
    {
        var position = Mask.AcceptChange(new(3, 1, []));

        var after = Mask.Slots;

        Assert.Equal(4, after.Count);
        Assert.Equal([0, 0, 0, null], after);
        Assert.Equal(2, position);
    }

    [Fact]
    public void EraseAllFromRight()
    {
        var position = Mask.AcceptChange(new(3, 4, []));

        var after = Mask.Slots;

        Assert.Equal(4, after.Count);
        Assert.Equal([null, null, null, null], after);
        Assert.Equal(0, position);
    }

    [Fact]
    public void EraseExcess()
    {
        var position = Mask.AcceptChange(new(3, 10, []));

        var after = Mask.Slots;

        Assert.Equal(4, after.Count);
        Assert.Equal([null, null, null, null], after);
        Assert.Equal(0, position);
    }

    [Fact]
    public void InsertLast()
    {
        var position = Mask.AcceptChange(new(3, 0, [1]));

        var after = Mask.Slots;

        Assert.Equal(4, after.Count);
        Assert.Equal([0, 0, 0, 1], after);
        Assert.Equal(3, position);
    }
}
