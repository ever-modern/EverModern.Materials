using System.Collections.Generic;
using System.Linq;
using DestallMaterials.WheelProtection.DataStructures.Text;

namespace DestallMaterials.Tests.XUnit;

using Digit = byte;

class SimpleTextConstraintsSpurce : ISlotConstraintsSource<Digit>
{
    static readonly SlotConstraint<Digit> _result = new(
        [.. Enumerable.Range(0, 10).Select(i => (Digit)i)]
    );

    public int Length => 4;

    public SlotConstraint<Digit> GetSlotConstraints(
        int index,
        IReadOnlyList<Digit> currentFilling
    ) => _result;
}

public class MaskTests
{
    readonly Mask<Digit> BasicMask;
    readonly Mask<char> ComplexMask;

    public MaskTests()
    {
        BasicMask = new(
            constraintsSource: new SimpleTextConstraintsSpurce(),
            initialSlots: [1, 1, 1, 1],
            equalityComparer: EqualityComparer<Digit>.Default
        );

        char[] numbers = ['*', '0', '1', '2', '3', '4'];
        var cSource = SlotConstraintsSource.Create<char>(
            (index, _) =>
                index == 3 ? new SlotConstraint<char>(['/']) : new SlotConstraint<char>(numbers),
            7
        );

        ComplexMask = new(cSource, [.. "***/***".OfType<char>()], EqualityComparer<char>.Default);
    }

    [Fact]
    public void InitMask()
    {
        var init = BasicMask.Slots;

        Assert.Equal(4, init.Count);
        Assert.Equal([1, 1, 1, 1], init);
    }

    [Fact]
    public void EraseRight()
    {
        var position = BasicMask.AcceptChange(new(3, 1, []));

        var after = BasicMask.Slots;

        Assert.Equal(4, after.Count);
        Assert.Equal([1, 1, 1, 0], after);
        Assert.Equal(3, position);
    }

    [Fact]
    public void EraseAllFromRight()
    {
        var position = BasicMask.AcceptChange(new(0, 4, []));

        var after = BasicMask.Slots;

        Assert.Equal(4, after.Count);
        Assert.Equal([0, 0, 0, 0], after);
        Assert.Equal(0, position);
    }

    [Fact]
    public void EraseExcess()
    {
        var position = BasicMask.AcceptChange(new(0, 10, []));

        var after = BasicMask.Slots;

        Assert.Equal(4, after.Count);
        Assert.Equal([0, 0, 0, 0], after);
        Assert.Equal(0, position);
    }

    [Fact]
    public void InsertLast()
    {
        var position = BasicMask.AcceptChange(new(3, 0, [2]));

        var after = BasicMask.Slots;

        Assert.Equal(4, after.Count);
        Assert.Equal([1, 1, 1, 2], after);
        Assert.Equal(4, position);
    }

    [Fact]
    public void InsertOneAfterAnother()
    {
        int position = 0;
        var fillAnother = (Digit symbol) =>
        {
            position = BasicMask.AcceptChange(new(position, 0, [symbol]));
        };

        fillAnother(2);
        Assert.Equal([2, 1, 1, 1], BasicMask.Slots);

        fillAnother(3);
        Assert.Equal([2, 3, 1, 1], BasicMask.Slots);

        fillAnother(4);
        Assert.Equal([2, 3, 4, 1], BasicMask.Slots);

        fillAnother(5);
        Assert.Equal([2, 3, 4, 5], BasicMask.Slots);
    }

    [Fact]
    public void EraseOneAfterAnother()
    {
        BasicMask.AcceptChange(new(0, 0, [1, 2, 3, 4]));

        Assert.Equal([1, 2, 3, 4], BasicMask.Slots);

        int position = 3;
        var eraseAnother = () =>
        {
            position = BasicMask.AcceptChange(new(At: position, Removed: 1, Inserted: [])) - 1;
        };

        eraseAnother();
        Assert.Equal([1, 2, 3, 0], BasicMask.Slots);

        eraseAnother();
        Assert.Equal([1, 2, 0, 0], BasicMask.Slots);

        eraseAnother();
        Assert.Equal([1, 0, 0, 0], BasicMask.Slots);

        eraseAnother();
        Assert.Equal([0, 0, 0, 0], BasicMask.Slots);
    }

    [Fact]
    public void PasteEntireValue()
    {
        const string inserted = "344/444";
        const string start = "***/***";
        const string finish = $"{inserted}{start}";
        var mask = ComplexMask;

        var change = ContentChange<char>.Get([.. start], [.. finish]);

        var position = mask.AcceptChange(change);

        Assert.Equal(7, position);
        Assert.Equal(inserted.ToCharArray(), mask.Slots);
    }
}