using System.Linq;
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

        var mask = new ImmutableMask<char>([.. from.ToString()], numberConstraints);

        var newMask = mask.Change(new(At: 0, Removed: 1, Inserted: ['2']), out var carretPosition);

        Assert.Equal(1, carretPosition);
        Assert.Equal([.. "2000"], newMask.ToArray());
    }

    [Fact]
    public void WriteOverflowingValue_MustWriteToEnd()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);

        var mask = new ImmutableMask<char>([.. from.ToString()], numberConstraints);

        var newMask = mask.Change(new(At: 0, Removed: 1, Inserted: ['8']), out var carretPosition);

        Assert.Equal(3, carretPosition);
        Assert.Equal([.. "1985"], newMask.ToArray());
    }

    [Fact]
    public void WriteTopEdgeNumber()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "2025"], numberConstraints);

        var newMask = mask.Change(new(At: 0, Removed: 1, Inserted: ['2']), out var carretPosition);

        Assert.Equal(1, carretPosition);
        Assert.Equal(['2', '0', '2', '5'], newMask.ToArray());
    }

    // Backspace Operations Tests
    [Fact]
    public void BackspaceFromBeginning()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "2000"], numberConstraints);

        var newMask = mask.Change(new(At: 1, Removed: 1, Inserted: []), out var carretPosition);

        Assert.Equal(1, carretPosition);
        Assert.Equal(['2', '0', '0', '0'], newMask.ToArray());
    }

    [Fact]
    public void BackspaceFromMiddle()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "2000"], numberConstraints);

        var newMask = mask.Change(new(At: 2, Removed: 1, Inserted: []), out var carretPosition);

        Assert.Equal(2, carretPosition);
        Assert.Equal(['2', '0', '0', '0'], newMask.ToArray());
    }

    [Fact]
    public void BackspaceFromEnd()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "2000"], numberConstraints);

        var newMask = mask.Change(new(At: 4, Removed: 1, Inserted: []), out var carretPosition);

        Assert.Equal(4, carretPosition);
        Assert.Equal(['2', '0', '0', '0'], newMask.ToArray());
    }

    // Delete Operations Tests
    [Fact]
    public void DeleteFromMiddle()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "2000"], numberConstraints);

        var newMask = mask.Change(new(At: 1, Removed: 1, Inserted: []), out var carretPosition);

        Assert.Equal(1, carretPosition);
        Assert.Equal(['2', '0', '0', '0'], newMask.ToArray());
    }

    [Fact]
    public void DeleteFromEnd()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "2000"], numberConstraints);

        var newMask = mask.Change(new(At: 3, Removed: 1, Inserted: []), out var carretPosition);

        Assert.Equal(3, carretPosition);
        Assert.Equal(['2', '0', '0', '0'], newMask.ToArray());
    }

    // Multi-character Insertion Tests
    [Fact]
    public void InsertMultipleCharacters()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "1975"], numberConstraints);

        var newMask = mask.Change(
            new(At: 0, Removed: 4, Inserted: ['1', '9', '8', '5']),
            out var carretPosition
        );

        Assert.Equal(4, carretPosition);
        Assert.Equal([.. "1985"], newMask.ToArray());
    }

    [Fact]
    public void InsertWithOverflow()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "1975"], numberConstraints);

        var newMask = mask.Change(
            new(At: 0, Removed: 2, Inserted: ['2', '0', '0', '0']),
            out var carretPosition
        );

        Assert.Equal(4, carretPosition);
        Assert.Equal([.. "2000"], newMask.ToArray());
    }

    // Range Replacement Tests
    [Fact]
    public void ReplaceMiddleRange()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "1975"], numberConstraints);

        var newMask = mask.Change(
            new(At: 1, Removed: 2, Inserted: ['9', '8']),
            out var carretPosition
        );

        Assert.Equal(3, carretPosition);
        Assert.Equal([.. "1985"], newMask.ToArray());
    }

    [Fact]
    public void ReplaceWithInvalidInput()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "1975"], numberConstraints);

        var newMask = mask.Change(
            new(At: 1, Removed: 2, Inserted: ['9', '9']),
            out var carretPosition
        );

        Assert.Equal(3, carretPosition);
        Assert.Equal(['1', '9', '9', '5'], newMask.ToArray());
    }

    // Boundary Condition Tests
    [Fact]
    public void InputAtMinimumBoundary()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "1975"], numberConstraints);

        var newMask = mask.Change(new(At: 0, Removed: 1, Inserted: ['1']), out var carretPosition);

        Assert.Equal(1, carretPosition);
        Assert.Equal([.. "1975"], newMask.ToArray());
    }

    [Fact]
    public void InputAtMaximumBoundary()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "2025"], numberConstraints);

        var newMask = mask.Change(new(At: 0, Removed: 1, Inserted: ['2']), out var carretPosition);

        Assert.Equal(1, carretPosition);
        Assert.Equal(['2', '0', '2', '5'], newMask.ToArray());
    }

    // Number Extension/Truncation Tests
    [Fact]
    public void ExtendNumber()
    {
        const int from = 100;
        const int to = 9999;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "1975"], numberConstraints);

        var newMask = mask.Change(new(At: 4, Removed: 0, Inserted: ['0']), out var carretPosition);

        Assert.Equal(4, carretPosition);
        Assert.Equal(['1', '9', '7', '5'], newMask.ToArray());
    }

    [Fact]
    public void TruncateNumber()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "1981"], numberConstraints);

        var newMask = mask.Change(new(At: 2, Removed: 2, Inserted: []), out var carretPosition);

        Assert.Equal(2, carretPosition);
        Assert.Equal([.. "1980"], newMask.ToArray());
    }

    // Edge Case Tests
    [Fact]
    public void EraseAllScenario()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "1975"], numberConstraints);

        var newMask = mask.Change(new(At: 0, Removed: 4, Inserted: []), out var carretPosition);

        Assert.Equal(0, carretPosition);
        Assert.Equal([.. "1975"], newMask.ToArray());
    }

    [Fact]
    public void ComplexPropagationScenario()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "1975"], numberConstraints);

        var newMask = mask.Change(new(At: 0, Removed: 1, Inserted: ['3']), out var carretPosition);

        Assert.Equal(0, carretPosition);
        Assert.Equal([.. "1975"], newMask.ToArray());
    }

    [Fact]
    public void Insert_PushOneSlotForth()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new ImmutableMask<char>([.. "2005"], numberConstraints);

        var newMask = mask.Change(new(At: 0, Removed: 0, Inserted: ['2']), out var carretPosition);

        Assert.Equal(3, carretPosition);
        Assert.Equal([.. "2025"], newMask.ToArray());
    }
}
