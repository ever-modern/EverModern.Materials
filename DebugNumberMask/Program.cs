using DestallMaterials.WheelProtection.DataStructures.Text;
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Number Mask Debug ===");
        
        // Test case 1: WriteOverflowingValue
        Console.WriteLine("\n--- Test 1: WriteOverflowingValue ---");
        const int from1 = 1975;
        const int to1 = 2025;
        var numberConstraints1 = new IntegerConstraintsSource(from1, to1);
        var mask1 = new Mask<char>(numberConstraints1, [.. from1.ToString()]);
        
        Console.WriteLine($"Initial state: '{string.Concat(mask1.Slots)}'");
        Console.WriteLine($"Range: {from1}-{to1}");
        Console.WriteLine("Operation: Replace position 0 with '2'");
        
        // Let's see what options are available at each position initially
        for (int i = 0; i < 4; i++)
        {
            var constraints = numberConstraints1.GetSlotConstraints(i, mask1.Slots);
            Console.WriteLine($"Position {i} options: [{string.Join(", ", constraints.Options)}]");
        }
        
        var carretPosition1 = mask1.AcceptChange(new ContentChange<char>(At: 0, Removed: 1, Inserted: ['2']));
        
        Console.WriteLine($"Final state: '{string.Concat(mask1.Slots)}'");
        Console.WriteLine($"Final number: {int.Parse(string.Concat(mask1.Slots))}");
        Console.WriteLine($"Final caret position: {carretPosition1}");
        Console.WriteLine($"Expected caret position: 0");
        Console.WriteLine($"Expected result: '2000'");
        
        // Test case 2: WriteOverflowingValue_MustWriteToEnd
        Console.WriteLine("\n--- Test 2: WriteOverflowingValue_MustWriteToEnd ---");
        const int from2 = 1975;
        const int to2 = 2025;
        var numberConstraints2 = new IntegerConstraintsSource(from2, to2);
        var mask2 = new Mask<char>(numberConstraints2, [.. from2.ToString()]);
        
        Console.WriteLine($"Initial state: '{string.Concat(mask2.Slots)}'");
        Console.WriteLine($"Range: {from2}-{to2}");
        Console.WriteLine("Operation: Replace position 0 with '8'");
        
        var carretPosition2 = mask2.AcceptChange(new ContentChange<char>(At: 0, Removed: 1, Inserted: ['8']));
        
        Console.WriteLine($"Final state: '{string.Concat(mask2.Slots)}'");
        Console.WriteLine($"Final number: {int.Parse(string.Concat(mask2.Slots))}");
        Console.WriteLine($"Final caret position: {carretPosition2}");
        Console.WriteLine($"Expected caret position: 3");
        Console.WriteLine($"Expected result: '1985'");
    }
}