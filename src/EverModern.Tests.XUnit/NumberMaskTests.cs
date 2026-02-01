using EverModern.WheelProtection.DataStructures.Text;

namespace EverModern.Tests.XUnit;

public class NumberMaskTests
{
    const int _from = 1975;
    const int _to = 2025;

    static IntegerMask CreateMask(long value) => new(value, _from, _to, 4);

    [Fact]
    public void WriteOverflowingValue()
    {
        var mask = CreateMask(_to);

        var newMask = mask.Change(new(At: 0, Removed: 0, Inserted: ['2']), out var caretPosition);

        Assert.Equal(2, caretPosition);
        Assert.Equal([.. $"{_to}"], newMask);
    }

    [Fact]
    public void ForceOne_AtStart_RecalcOther()
    {
        var mask = CreateMask(_from);

        var newMask = mask.Change(new(At: 0, Removed: 0, Inserted: ['1']), out var caretPosition);

        Assert.Equal(2, caretPosition);
        Assert.Equal([.. "1975"], newMask);
    }

    [Fact]
    public void ForceOne_AtSecondDigit_RecalcOther()
    {
        var mask = CreateMask(_to);

        var newMask = mask.Change(new(At: 1, Removed: 0, Inserted: ['9']), out var caretPosition);

        Assert.Equal([.. _to.ToString()], newMask);
        Assert.Equal(1, caretPosition);
    }

    [Fact]
    public void WriteOverflowingValue_MustWriteToEnd()
    {
        var mask = CreateMask(_from);

        var newMask = mask.Change(new(At: 2, Removed: 0, Inserted: ['8']), out var caretPosition);

        Assert.Equal(3, caretPosition);
        Assert.Equal([.. "1985"], newMask);
    }

    [Fact]
    public void WriteTopEdgeNumber()
    {
        var mask = CreateMask(_to);

        var newMask = mask.Change(new(At: 0, Removed: 0, Inserted: ['2']), out var caretPosition);

        Assert.Equal(2, caretPosition);
        Assert.Equal(['2', '0', '2', '5'], newMask);
    }

    // Backspace Operations Tests
    [Fact]
    public void DeleteToBeginning()
    {
        var mask = CreateMask(2000);

        ContentChange<char> change = new(At: 2, Removed: 1, Inserted: []);

        var newMask = mask.Change(change, out var caretPosition);

        Assert.Equal(0, caretPosition);
        Assert.Equal(['2', '0', '0', '0'], newMask);
    }

    [Fact]
    public void DeleteFromSecondSlot()
    {
        var mask = CreateMask(2000);

        var newMask = mask.Change(new(At: 2, Removed: 1, Inserted: []), out var caretPosition);

        Assert.Equal(0, caretPosition);
        Assert.Equal(['2', '0', '0', '0'], newMask);
    }

    [Fact]
    public void BackspaceFromEnd()
    {
        var mask = CreateMask(2000);

        ContentChange<char> change = new(At: 3, Removed: 1, Inserted: []);
        var newMask = mask.Change(change, out var caretPosition);

        Assert.Equal(2, caretPosition);
        Assert.Equal(['2', '0', '0', '0'], newMask);
    }

    // Delete Operations Tests
    [Fact]
    public void DeleteFromMiddle()
    {
        var mask = CreateMask(2000);

        var newMask = mask.Change(new(At: 1, Removed: 1, Inserted: []), out var caretPosition);

        Assert.Equal(0, caretPosition);
        Assert.Equal(['2', '0', '0', '0'], newMask);
    }

    [Fact]
    public void DeleteFromEnd()
    {
        var mask = CreateMask(2000);

        ContentChange<char> change = new(At: 3, Removed: 1, Inserted: []);
        var newMask = mask.Change(change, out var caretPosition);

        Assert.Equal(2, caretPosition);
        Assert.Equal(['2', '0', '0', '0'], newMask);
    }

    // Multi-character Insertion Tests
    [Fact]
    public void InsertMultipleCharacters()
    {
        var mask = CreateMask(1975);

        var newMask = mask.Change(
            new(At: 0, Removed: 4, Inserted: ['1', '9', '8', '5']),
            out var caretPosition
        );

        Assert.Equal(4, caretPosition);
        Assert.Equal([.. "1985"], newMask);
    }

    [Fact]
    public void InsertWithOverflow()
    {
        var mask = CreateMask(1975);

        var newMask = mask.Change(
            new(At: 0, Removed: 2, Inserted: ['2', '0', '0', '0']),
            out var caretPosition
        );

        Assert.Equal(4, caretPosition);
        Assert.Equal([.. "2000"], newMask);
    }

    // Range Replacement Tests
    [Fact]
    public void ReplaceMiddleRange()
    {
        var mask = CreateMask(1975);

        var newMask = mask.Change(
            new(At: 1, Removed: 2, Inserted: ['9', '8']),
            out var caretPosition
        );

        Assert.Equal(3, caretPosition);
        Assert.Equal([.. "1985"], newMask);
    }

    [Fact]
    public void ReplaceWithInvalidInput()
    {
        var mask = CreateMask(1975);

        var newMask = mask.Change(
            new(At: 1, Removed: 2, Inserted: ['9', '9']),
            out var caretPosition
        );

        Assert.Equal(3, caretPosition);
        Assert.Equal(['1', '9', '9', '5'], newMask);
    }

    // Boundary Condition Tests
    [Fact]
    public void InputAtMinimumBoundary()
    {
        var mask = CreateMask(1975);

        var newMask = mask.Change(new(At: 0, Removed: 1, Inserted: ['1']), out var caretPosition);

        Assert.Equal(2, caretPosition);
        Assert.Equal([.. "1975"], newMask);
    }

    [Fact]
    public void InputAtMaximumBoundary()
    {
        var mask = CreateMask(2025);

        var newMask = mask.Change(new(At: 0, Removed: 1, Inserted: ['2']), out var caretPosition);

        Assert.Equal(2, caretPosition);
        Assert.Equal(['2', '0', '2', '5'], newMask);
    }

    [Fact]
    public void TruncateNumber()
    {
        var mask = CreateMask(1981);

        var newMask = mask.Change(new(At: 2, Removed: 2, Inserted: []), out var caretPosition);

        Assert.Equal(0, caretPosition);
        Assert.Equal([.. "1980"], newMask);
    }

    // Edge Case Tests
    [Fact]
    public void EraseAllScenario()
    {
        var mask = CreateMask(1975);

        var newMask = mask.Change(new(At: 0, Removed: 4, Inserted: []), out var caretPosition);

        Assert.Equal(0, caretPosition);
        Assert.Equal([.. "1975"], newMask);
    }

    [Fact]
    public void ComplexPropagationScenario()
    {
        var mask = CreateMask(1975);

        var newMask = mask.Change(new(At: 0, Removed: 1, Inserted: ['3']), out var caretPosition);

        Assert.Equal(0, caretPosition);
        Assert.Equal([.. "1975"], newMask);
    }

    [Fact]
    public void Insert_PushOneSlotForth()
    {
        var mask = CreateMask(2005);

        var newMask = mask.Change(new(At: 1, Removed: 0, Inserted: ['2']), out var caretPosition);

        Assert.Equal(3, caretPosition);
        Assert.Equal([.. "2025"], newMask);
    }

    [Fact]
    public void Insert9_AtTheEnd()
    {
        var mask = CreateMask(2000);

        var newMask = mask.Change(new(At: 3, Removed: 0, Inserted: ['9']), out var caretPosition);

        Assert.Equal(4, caretPosition);
        Assert.Equal([.. "2009"], newMask);
    }
}
