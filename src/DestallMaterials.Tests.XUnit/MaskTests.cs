using DestallMaterials.WheelProtection.DataStructures.Text;
using System.Linq;
using System.Collections.Generic;

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

    public int Length => 4;

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

public class DateMaskTests
{
    // Helper to create a fresh date mask with8 slots (ddMMyyyy)
    Mask<char> CreateDateMask()
    {
        var initial = Enumerable.Repeat<char?>(null, 8).ToArray();
        return new Mask<char>(new DateSlotConstraintsSource(new WheelProtection.DataStructures.Time.DateTimeRange(default, DateTime.Now)), initial, EqualityComparer<char?>.Default);
    }

    [Fact]
    public void DateConstraints_February_LeapYear_RestrictsDayTens()
    {
        var source = new DateSlotConstraintsSource(new WheelProtection.DataStructures.Time.DateTimeRange(default, DateTime.Now));

        // Fill month02 and year2000
        var filling = new char?[] { null, null, '0', '2', '2', '0', '0', '0' };
        var constraints = source.GetConstraints(filling);

        var dayTens = constraints[0].Options.Select(c => c!.Value).ToArray();

        // For February in leap year max day is29 -> day tens '3' should not be allowed
        Assert.DoesNotContain('3', dayTens);
        // day tens should allow0,1,2
        Assert.Contains('0', dayTens);
        Assert.Contains('1', dayTens);
        Assert.Contains('2', dayTens);
    }

    [Fact]
    public void DateMask_InsertValidDate_WritesSlotsAndReturnsCaret()
    {
        var mask = CreateDateMask();

        // Insert month '03'
        var pos1 = mask.AcceptChange(new(2, 0, ['0', '3']));
        // Insert year '2001'
        var pos2 = mask.AcceptChange(new(4, 0, ['2', '0', '0', '1']));
        // Insert day '05'
        var pos3 = mask.AcceptChange(new(0, 0, ['0', '5']));

        var slots = mask.Slots;

        Assert.Equal('0', slots[0]);
        Assert.Equal('5', slots[1]);
        Assert.Equal('0', slots[2]);
        Assert.Equal('3', slots[3]);

        // caret after last two digits should be index1 (last filled position)
        Assert.Equal(1, pos3);
    }

    [Fact]
    public void DateMask_InsertInvalidDay_IsNotPlacedInDaySlot()
    {
        var mask = CreateDateMask();

        // Set month =02 and year =2001 (non-leap -> max day28)
        mask.AcceptChange(new(2, 0, ['0', '2']));
        mask.AcceptChange(new(4, 0, ['2', '0', '0', '1']));

        // Try to insert day '30' which is invalid for Feb2001
        var pos = mask.AcceptChange(new(0, 0, ['3', '0']));

        var slots = mask.Slots;

        // Day tens should not be '3'
        Assert.True(slots[0] != '3');
    }

    [Fact]
    public void Autoset_DeterministicSlots_AreFilledAutomatically()
    {
        // Deterministic constraints: slot1 and slot2 have single allowed option
        var source = new DeterministicConstraintsSource();
        var initial = Enumerable.Repeat<char?>(null, 4).ToArray();
        var mask = new Mask<char>(source, initial, EqualityComparer<char?>.Default);

        // Trigger autoset by calling AcceptChange with no-op change
        var caret = mask.AcceptChange(new(0, 0, []));

        var slots = mask.Slots;
        Assert.Equal('X', slots[1]);
        Assert.Equal('Y', slots[2]);
    }

    class DeterministicConstraintsSource : ISlotConstraintsSource<char>
    {
        public int Length => 4;

        public IReadOnlyList<SlotConstraint<char>> GetConstraints(IReadOnlyList<char?> currentFilling)
        {
            return [
                new SlotConstraint<char>(['0','1','2']),
                new SlotConstraint<char>(['X']),
                new SlotConstraint<char>(['Y']),
                new SlotConstraint<char>(['A','B','C'])
            ];
        }
    }
}
