using DestallMaterials.WheelProtection.DataStructures.Text;
using DestallMaterials.WheelProtection.DataStructures.Time;

namespace DestallMaterials.Tests.XUnit;

public class DateMaskTests
{
    // Adapter to bridge new DateSlotConstraintsSource (returns char[] per slot)
    // to ISlotConstraintsSource<char> (returns SlotConstraint<char> per slot) used by Mask
    class DateSlotConstraintsAdapter : ISlotConstraintsSource<char>
    {
        readonly DateSlotConstraintsSource _inner;

        public DateSlotConstraintsAdapter(DateTimeRange range, DateFormatting formatting)
        {
            _inner = new DateSlotConstraintsSource(range, formatting);
        }

        public int Length => _inner.Length;

        public SlotConstraint<char> GetSlotConstraints(
            int slotIndex,
            IReadOnlyList<char> currentFilling
        )
        {
            var opts = _inner.GetSlotConstraints(slotIndex, currentFilling);
            return opts;
        }
    }

    // Helper to create a fresh date mask with8 slots (ddMMyyyy)
    Mask<char> CreateDateMask()
    {
        // dd.MM.yyyy -> 10 slots
        var initial = Enumerable.Repeat(default(char), 10).ToArray();
        return new Mask<char>(
            new DateSlotConstraintsAdapter(
                new DateTimeRange(default, DateTime.Now),
                DateFormatting.Parse("dd.MM.yyyy")
            ),
            initial,
            EqualityComparer<char>.Default
        );
    }

    [Fact]
    public void DateConstraints_February_LeapYear_RestrictsDayTens()
    {
        var adapter = new DateSlotConstraintsAdapter(
            new DateTimeRange(default, DateTime.Now),
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

        var constraints = Enumerable
            .Range(0, adapter.Length)
            .Select(i => adapter.GetSlotConstraints(i, filling))
            .ToArray();

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
        var options = Enumerable
            .Range(0, source.Length)
            .Select(i => source.GetSlotConstraints(i, []))
            .ToArray();
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

        public SlotConstraint<char> GetSlotConstraints(int index, IReadOnlyList<char> _) =>
            index switch
            {
                0 => new SlotConstraint<char>(['0', '1', '2']),
                1 => new SlotConstraint<char>(['X']),
                2 => new SlotConstraint<char>(['Y']),
                3 => new SlotConstraint<char>(['A', 'B', 'C']),
                _ => new SlotConstraint<char>(['0']),
            };
    }
}
