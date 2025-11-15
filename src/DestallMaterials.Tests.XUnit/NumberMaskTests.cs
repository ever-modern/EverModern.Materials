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

        Assert.Equal(0, carretPosition);
        Assert.Equal(['0', '9', '9', '9'], mask.Slots);
    }

    [Fact]
    public void WriteTopEdgeNumber()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "2025"]);

        var carretPosition = mask.AcceptChange(new(At: 0, Removed: 1, Inserted: ['2']));

        Assert.Equal(1, carretPosition);
        Assert.Equal(['2', '0', '2', '0'], mask.Slots);
    }

    // Backspace Operations Tests
    [Fact]
    public void BackspaceFromBeginning()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "2000"]);

        var carretPosition = mask.AcceptChange(new(At: 1, Removed: 1, Inserted: []));

        Assert.Equal(1, carretPosition);
        Assert.Equal(['2', '\0', '0', '0'], mask.Slots);
    }

    [Fact]
    public void BackspaceFromMiddle()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "2000"]);

        var carretPosition = mask.AcceptChange(new(At: 2, Removed: 1, Inserted: []));

        Assert.Equal(2, carretPosition);
        Assert.Equal(['2', '0', '\0', '0'], mask.Slots);
    }

    [Fact]
    public void BackspaceFromEnd()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "2000"]);

        var carretPosition = mask.AcceptChange(new(At: 4, Removed: 1, Inserted: []));

        Assert.Equal(4, carretPosition);
        Assert.Equal(['2', '0', '0', '\0'], mask.Slots);
    }

    // Delete Operations Tests
    [Fact]
    public void DeleteFromMiddle()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "2000"]);

        var carretPosition = mask.AcceptChange(new(At: 1, Removed: 1, Inserted: []));

        Assert.Equal(1, carretPosition);
        Assert.Equal(['2', '\0', '0', '0'], mask.Slots);
    }

    [Fact]
    public void DeleteFromEnd()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "2000"]);

        var carretPosition = mask.AcceptChange(new(At: 3, Removed: 1, Inserted: []));

        Assert.Equal(3, carretPosition);
        Assert.Equal(['2', '0', '0', '\0'], mask.Slots);
    }

    // Multi-character Insertion Tests
    [Fact]
    public void InsertMultipleCharacters()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "1975"]);

        var carretPosition = mask.AcceptChange(new(At: 0, Removed: 4, Inserted: ['1', '9', '8', '5']));

        Assert.Equal(4, carretPosition);
        Assert.Equal([.. "1985"], mask.Slots);
    }

    [Fact]
    public void InsertWithOverflow()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "1975"]);

        var carretPosition = mask.AcceptChange(new(At: 0, Removed: 2, Inserted: ['2', '0', '0', '0']));

        Assert.Equal(0, carretPosition);
        Assert.Equal(['2', '0', '0', '0'], mask.Slots);
    }

    // Range Replacement Tests
    [Fact]
    public void ReplaceMiddleRange()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "1975"]);

        var carretPosition = mask.AcceptChange(new(At: 1, Removed: 2, Inserted: ['9', '8']));

        Assert.Equal(3, carretPosition);
        Assert.Equal([.. "1985"], mask.Slots);
    }

    [Fact]
    public void ReplaceWithInvalidInput()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "1975"]);

        var carretPosition = mask.AcceptChange(new(At: 1, Removed: 2, Inserted: ['9', '9']));

        Assert.Equal(3, carretPosition);
        Assert.Equal(['1', '9', '9', '5'], mask.Slots);
    }

    // Boundary Condition Tests
    [Fact]
    public void InputAtMinimumBoundary()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "1975"]);

        var carretPosition = mask.AcceptChange(new(At: 0, Removed: 1, Inserted: ['1']));

        Assert.Equal(1, carretPosition);
        Assert.Equal([.. "1975"], mask.Slots);
    }

    [Fact]
    public void InputAtMaximumBoundary()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "2025"]);

        var carretPosition = mask.AcceptChange(new(At: 0, Removed: 1, Inserted: ['2']));

        Assert.Equal(1, carretPosition);
        Assert.Equal(['2', '0', '2', '0'], mask.Slots);
    }

    // Number Extension/Truncation Tests
    [Fact]
    public void ExtendNumber()
    {
        const int from = 100;
        const int to = 9999;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "1975"]);

        var carretPosition = mask.AcceptChange(new(At: 4, Removed: 0, Inserted: ['0']));

        Assert.Equal(5, carretPosition);
        Assert.Equal(['1', '9', '7', '5'], mask.Slots);
    }

    [Fact]
    public void TruncateNumber()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "1975"]);

        var carretPosition = mask.AcceptChange(new(At: 2, Removed: 2, Inserted: []));

        Assert.Equal(2, carretPosition);
        Assert.Equal("19", string.Concat(mask.Slots.TakeWhile(c => c != default)));
    }

    // Edge Case Tests
    [Fact]
    public void EmptyInputScenario()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "1975"]);

        var carretPosition = mask.AcceptChange(new(At: 0, Removed: 4, Inserted: []));

        Assert.Equal(0, carretPosition);
        Assert.All(mask.Slots, slot => Assert.Equal(default(char), slot));
    }

    [Fact]
    public void ComplexPropagationScenario()
    {
        const int from = 1975;
        const int to = 2025;

        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, initialSlots: [.. "1975"]);

        // Insert '3' at position 0 should cause propagation through the number
        var carretPosition = mask.AcceptChange(new(At: 0, Removed: 1, Inserted: ['3']));

        Assert.Equal(0, carretPosition);
        Assert.Equal(['2', '0', '0', '0'], mask.Slots);
    }

    [Fact]
    public void DebugDeletionBehavior()
    {
        var numberConstraints = new IntegerConstraintsSource(1975, 2025);
        var mask = new Mask<char>(numberConstraints, initialSlots: "1975".ToCharArray());

        System.Console.WriteLine("Initial state:");
        System.Console.WriteLine($"Slots: [{string.Join(", ", mask.Slots.Select(c => $"'{c}'"))}]");

        var carretPosition = mask.AcceptChange(new(At: 0, Removed: 4, Inserted: System.Array.Empty<char>()));

        System.Console.WriteLine("\nAfter deleting all 4 characters:");
        System.Console.WriteLine($"Cursor: {carretPosition}");
        System.Console.WriteLine($"Slots: [{string.Join(", ", mask.Slots.Select(c => $"'{c}'"))}]");
        System.Console.WriteLine($"Slots as string: '{new string(mask.Slots.Select(c => c == '\0' ? '·' : c).ToArray())}'");
        
        // Expected: all slots should be '\0'
        Assert.All(mask.Slots, slot => Assert.Equal('\0', slot));
    }
}
