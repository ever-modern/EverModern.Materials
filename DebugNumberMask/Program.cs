using DestallMaterials.WheelProtection.DataStructures.Text;
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Number Mask Debug ===");
        
        // Test case: Insert_PushOneSlotForth
        Console.WriteLine("\n--- Test: Insert_PushOneSlotForth ---");
        const int from = 1975;
        const int to = 2025;
        var numberConstraints = new IntegerConstraintsSource(from, to);
        var mask = new Mask<char>(numberConstraints, [.. "2005"]);
        
        Console.WriteLine($"Initial state: '{string.Concat(mask.Slots)}'");
        Console.WriteLine($"Range: {from}-{to}");
        Console.WriteLine("Operation: At=0, Removed=0, Inserted=['2']");
        
        // Let's see what options are available at each position initially
        Console.WriteLine("\nOptions for each position with '2005':");
        for (int i = 0; i < 4; i++)
        {
            var constraints = numberConstraints.GetSlotConstraints(i, mask.Slots);
            Console.WriteLine($"Position {i} options: [{string.Join(", ", constraints.Options)}]");
        }
        
        var carretPosition = mask.AcceptChange(new ContentChange<char>(At: 0, Removed: 0, Inserted: ['2']));
        
        Console.WriteLine($"\nFinal state: '{string.Concat(mask.Slots)}'");
        Console.WriteLine($"Final caret position: {carretPosition}");
        Console.WriteLine($"Expected: '2025', caret at 3");
        
        // Additional test: Let's trace what happens when we insert '2' manually at each position
        Console.WriteLine("\n--- Tracing '2' placement possibilities ---");
        
        // Fresh mask
        mask = new Mask<char>(numberConstraints, [.. "2005"]);
        char[] testSlots = ['2', '0', '0', '5'];
        
        // Check if '2' is allowed at slot 0
        var opts0 = SlotOptionFunctions.GetOptionsForSlot(0, testSlots.AsSpan(), 4, from, to);
        Console.WriteLine($"Can '2' go at slot 0? Options: [{string.Join(",", opts0)}] -> {opts0.Contains('2')}");
        
        // Let's check what happens if we insert '2' and push everything right
        // If '2' is inserted at position 0, it should push '2' from pos 0 to pos 1
        // Result would be: ['2', '2', '0', '0'] with '5' pushed out -> but that's invalid
        // Actually, inserting without removal means we're just inserting, so slots stay same length
        
        // The expected behavior: since '2' can be at slot 0, it stays there
        // The old '2' at slot 0 needs to go somewhere...
        
        // Actually, Re-reading the test:
        // At=0, Removed=0, Inserted=['2'] 
        // This means insert '2' at position 0 without removing anything
        // Current: "2005"
        // After insert at 0: "22005" but mask is fixed length 4
        // So it should push digits to the right
        // Result should be: 2->0, 0->0, 0->5 pushed out? No...
        
        // Let me think about this differently:
        // The user types '2' at position 0 in "2005" 
        // Since '2' is already there, and we're inserting (not replacing), 
        // the digits should shift right: 2005 -> 2 pushed in at 0, 005 shifts right but last digit is pushed out
        // So it becomes: 2, 2, 0, 0 with 5 pushed out? But that may not be valid
        
        // Actually looking at the expected output "2025":
        // This suggests: insert '2' at position 0, pushing everything right
        // 2005 -> the '2' stays at pos 0, the '0' at pos 1 should become something
        // 
        // Wait, I think I misunderstand. Let's trace:
        // If we insert '2' at pos 0 (no removal), and slots are fixed length 4:
        // - Current value: 2005
        // - Inserting '2' at pos 0 should shift values right (last value drops)
        // - Would become: 2 2 0 0 (with 5 dropped), but that's 2200 which is out of range
        // 
        // But expected is "2025" - this means the mask is adjusting values!
        // 2 inserted at 0, existing 2 moves to... wait, slot 2?
        // Original: [2,0,0,5]
        // Expected: [2,0,2,5]
        // 
        // So '2' was inserted at slot 0, but it was already there.
        // The PUSH happens: looking for where the existing '2' can go
        // It can't stay at pos 0 (blocked by new '2')
        // It can't go to pos 1 (options for pos 1 with 2xxx are only '0')
        // It CAN go to pos 2 (options: 0,1,2,3,4,5) -> '2' is valid!
        // So the '2' lands at pos 2
        // Final: [2,0,2,5] = 2025, caret at position 3 (after the pushed '2')
        
        Console.WriteLine("\n--- Manual tracing ---");
        char[] slotsA = ['2', '0', '0', '5'];
        
        // Inserting '2' at pos 0
        // Slot 0 options:
        var optsSlot0 = SlotOptionFunctions.GetOptionsForSlot(0, slotsA.AsSpan(), 4, from, to);
        Console.WriteLine($"Slot 0 with '2005': [{string.Join(",", optsSlot0)}]");
        
        // If '2' is already at slot 0 and we're inserting (not replacing), what should happen?
        // The old '2' needs to move. Check slot 1:
        char[] slotsB = ['2', '2', '0', '5']; // hypothetical after '2' moved to slot 1
        var optsSlot1 = SlotOptionFunctions.GetOptionsForSlot(1, slotsB.AsSpan(), 4, from, to);
        Console.WriteLine($"Slot 1 with '2205': [{string.Join(",", optsSlot1)}]");
        
        // Check slot 2:
        char[] slotsC = ['2', '0', '2', '5']; // hypothetical with '2' at slot 2
        var optsSlot2 = SlotOptionFunctions.GetOptionsForSlot(2, slotsC.AsSpan(), 4, from, to);
        Console.WriteLine($"Slot 2 with '2025': [{string.Join(",", optsSlot2)}]");
        
        // Check if 2025 is valid
        Console.WriteLine($"2025 is in range [{from}-{to}]: {2025 >= from && 2025 <= to}");
    }
}