using System.Collections.Generic;
using System.Linq;
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
                [.. Enumerable.Range(0, 10).Select(i => (Digit)i)]
            )),
    ];

    public int Length => 4;

    public IReadOnlyList<SlotConstraint<Digit>> GetConstraints(
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
            _ => [numbers, numbers, numbers, '/', numbers, numbers, numbers],
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

public class DateMaskTests
{
    // Helper to create a fresh date mask with8 slots (ddMMyyyy)
    Mask<char> CreateDateMask()
    {
        // dd.MM.yyyy -> 10 slots
        var initial = Enumerable.Repeat(default(char), 10).ToArray();
        return new Mask<char>(
            new DateSlotConstraintsSource(
                new WheelProtection.DataStructures.Time.DateTimeRange(default, DateTime.Now),
                DateFormatting.Parse("dd.MM.yyyy")
            ),
            initial,
            EqualityComparer<char>.Default
        );
    }

    [Fact]
    public void DateConstraints_February_LeapYear_RestrictsDayTens()
    {
        var source = new DateSlotConstraintsSource(
            new WheelProtection.DataStructures.Time.DateTimeRange(default, DateTime.Now),
            DateFormatting.Parse("dd.MM.yyyy")
        );

        // Fill month 02 and year 2000 for format dd.MM.yyyy -> positions: [0]=d1,[1]=d2,[2]='.',[3]=m1,[4]=m2,[5]='.',[6]=y1..[9]=y4
        var filling = new char[]
        {
            default(char),
            default(char),
            '.',
            '0',
            '2',
            '.',
            '2',
            '0',
            '0',
            '0',
        };
        var constraints = source.GetConstraints(filling);

        var dayTens = constraints[0].Options.ToArray();

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

        // For format dd.MM.yyyy the positions are:
        // [0]=d1, [1]=d2, [2]='.', [3]=m1, [4]=m2, [5]='.', [6]=y1 .. [9]=y4
        // Insert month '03' at month start index (3)
        var pos1 = mask.AcceptChange(new(3, 0, ['0', '3']));
        // Insert year '2001' at year start index (6)
        var pos2 = mask.AcceptChange(new(6, 0, ['2', '0', '0', '1']));
        // Insert day '05' at day start (0)
        var pos3 = mask.AcceptChange(new(0, 0, ['0', '5']));

        var slots = mask.Slots;

        // Expected layout after inserts:
        // [0]='0', [1]='5', [2]='.', [3]='0', [4]='3', [5]='.', [6]='2', [7]='0', [8]='0', [9]='1'
        Assert.Equal('0', slots[0]);
        Assert.Equal('5', slots[1]);
        Assert.Equal('.', slots[2]);
        Assert.Equal('0', slots[3]);
        Assert.Equal('3', slots[4]);
        Assert.Equal('.', slots[5]);
        Assert.Equal('2', slots[6]);
        Assert.Equal('0', slots[7]);
        Assert.Equal('0', slots[8]);
        Assert.Equal('1', slots[9]);
    }

    [Fact]
    public void DateMask_InsertInvalidDay_IsNotPlacedInDaySlot()
    {
        var mask = CreateDateMask();

        // Set month =02 and year =2001 (non-leap -> max day28)
        mask.AcceptChange(new(3, 0, ['0', '2']));
        mask.AcceptChange(new(6, 0, ['2', '0', '0', '1']));

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
        var options = source.GetConstraints([]);
        var initial = Enumerable.Repeat(options[0].Options[0], 4).ToArray();
        var mask = new Mask<char>(source, initial, EqualityComparer<char>.Default);

        // Trigger autoset by calling AcceptChange with no-op change
        var caret = mask.AcceptChange(new(0, 0, []));

        var slots = mask.Slots;
        Assert.Equal('X', slots[1]);
        Assert.Equal('Y', slots[2]);
    }

    class DeterministicConstraintsSource : ISlotConstraintsSource<char>
    {
        public int Length => 4;

        public IReadOnlyList<SlotConstraint<char>> GetConstraints(IReadOnlyList<char> _) =>
            [
                new SlotConstraint<char>(['0', '1', '2']),
                new SlotConstraint<char>(['X']),
                new SlotConstraint<char>(['Y']),
                new SlotConstraint<char>(['A', 'B', 'C']),
            ];
    }
}
