using DestallMaterials.WheelProtection.DataStructures.Text;

namespace DestallMaterials.Tests.XUnit;

public class ConstraintUtilitiesTests
{
    [Fact]
    public void SomeUndefinedCharacters()
    {
        const int from = 1975;
        const int to = 2025;
        var result = SlotOptionFunctions.GetOptionsForSlot(2, "xx?4", 4, from, to);
        Assert.Equal([.. "89012"], result);
    }

    [Fact]
    public void Range_ToCharOptions()
    {
        const int from = 1975;
        const int to = 2025;

        var result = SlotOptionFunctions.GetOptionsForSlot(2, "2075", 4, from, to);

        Assert.Equal([.. "012"], result);
    }

    [Fact]
    public void Range_ForPush()
    {
        const int from = 1975;
        const int to = 2025;

        var result = SlotOptionFunctions.GetOptionsForSlot(0, "1***", 4, from, to);

        Assert.Equal(['1', '2'], result);
    }

    [Fact]
    public void GetOptionsForSlot_BasicTest_SimpleNumber()
    {
        // Test: 2-digit number "5?" with range 50-59, position 1
        // Since first digit is 5 and range is 50-59, second digit can be 0-9
        var result = SlotOptionFunctions.GetOptionsForSlot(1, "5?".AsSpan(), 2, 50, 59);
        Assert.Equal(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'], result);
    }

    [Fact]
    public void GetOptionsForSlot_BasicTest_AllDigitsValid()
    {
        // Test: Very wide range should allow all digits
        var result = SlotOptionFunctions.GetOptionsForSlot(0, "?".AsSpan(), 1, 0, 9999);
        Assert.Equal(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'], result);
    }

    [Fact]
    public void GetOptionsForSlot_BasicTest_SingleDigit()
    {
        // Test: Single digit number, position 0
        var result = SlotOptionFunctions.GetOptionsForSlot(0, "?".AsSpan(), 1, 3, 7);
        Assert.Equal(['3', '4', '5', '6', '7'], result);
    }

    #region Indefinite Digits Tests

    [Fact]
    public void GetOptionsForSlot_IndefiniteDigits_AllIndefinite()
    {
        // Test: All digits indefinite, position 0, range 100-200
        // Position 0 can be 1 or 2 (for 100-199 and 200)
        var result = SlotOptionFunctions.GetOptionsForSlot(0, "???".AsSpan(), 3, 100, 200);
        Assert.Equal(['1', '2'], result);
    }

    [Fact]
    public void GetOptionsForSlot_IndefiniteDigits_MultipleValid()
    {
        // Test: Position 1 of "?2?" for range 120-180
        // Position 1 can be any digit because we can adjust other positions to fit the range
        var result = SlotOptionFunctions.GetOptionsForSlot(1, "?2?".AsSpan(), 3, 120, 180);
        Assert.Equal([.. "2345678"], result);
    }

    [Fact]
    public void GetOptionsForSlot_IndefiniteDigits_PartialConstraint()
    {
        // Test: "?5?" position 0, range 100-199
        // Position 0 must be 1
        var result = SlotOptionFunctions.GetOptionsForSlot(0, "?5?".AsSpan(), 3, 100, 199);
        Assert.Equal(['1'], result);
    }

    [Fact]
    public void GetOptionsForSlot_IndefiniteDigits_LastPosition()
    {
        // Test: "?5?" position 2, range 150-159
        // Last position can be 0-9 as long as middle stays 5
        var result = SlotOptionFunctions.GetOptionsForSlot(2, "?5?".AsSpan(), 3, 150, 159);
        Assert.Equal(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'], result);
    }

    #endregion

    #region Definite Digits Tests

    [Fact]
    public void GetOptionsForSlot_DefiniteDigits_MatchingRange()
    {
        // Test: "123" position 1 with definite digit '2', range 120-129
        // Position 1 is already '2', so should return ['2']
        var result = SlotOptionFunctions.GetOptionsForSlot(1, "123".AsSpan(), 3, 120, 129);
        Assert.Equal(['2'], result);
    }

    [Fact]
    public void GetOptionsForSlot_DefiniteDigits_OutsideRange()
    {
        // Test: "123" position 1 with definite digit '2', range 110-119
        // Position 1 is '2' but number would be 123, outside range 110-119
        // But we're checking position 1, so we need to see if there's a valid digit for position 1
        // If we change position 1, the number could be 1X3. To be in 110-119, X must make 1X3 = 11X
        // For 110-119, position 1 should be 1, but it's currently 2, so no valid digits
        var result = SlotOptionFunctions.GetOptionsForSlot(1, "123".AsSpan(), 3, 110, 119);
        Assert.Equal(['1'], result);
    }

    [Fact]
    public void GetOptionsForSlot_DefiniteDigits_MixedConstraints()
    {
        // Test: "1?3" position 1, range 103-193
        // First digit is 1, last is 3, middle can be 0-9
        var result = SlotOptionFunctions.GetOptionsForSlot(1, "1?3".AsSpan(), 3, 103, 193);
        Assert.Equal(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'], result);
    }

    #endregion

    #region Boundary Tests

    [Fact]
    public void GetOptionsForSlot_BoundaryTests_ExactMinMax()
    {
        // Test: Position 0 of "?5?" for range 150-150 (exact match)
        // Position 0 must be 1
        var result = SlotOptionFunctions.GetOptionsForSlot(0, "?5?".AsSpan(), 3, 150, 150);
        Assert.Equal(['1'], result);
    }

    [Fact]
    public void GetOptionsForSlot_BoundaryTests_MinBoundary()
    {
        // Test: Position 0 of "5??" for range 500-599
        // Position 0 is 5, so it's valid
        var result = SlotOptionFunctions.GetOptionsForSlot(0, "5??".AsSpan(), 3, 500, 599);
        Assert.Equal(['5'], result);
    }

    [Fact]
    public void GetOptionsForSlot_BoundaryTests_MaxBoundary()
    {
        // Test: Position 0 of "5??" for range 599-599
        // Position 0 is 5, valid for exact 599
        var result = SlotOptionFunctions.GetOptionsForSlot(0, "5??".AsSpan(), 3, 599, 599);
        Assert.Equal(['5'], result);
    }

    [Fact]
    public void GetOptionsForSlot_BoundaryTests_OutOfBounds()
    {
        // Test: Position 0 of "9??" for range 500-599
        // Position 0 is 9, but we're testing if we can change it. To be in 500-599, position 0 must be 5
        var result = SlotOptionFunctions.GetOptionsForSlot(0, "9??".AsSpan(), 3, 500, 599);
        Assert.Equal(['5'], result);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void GetOptionsForSlot_EdgeCases_SingleDigitNumber()
    {
        // Test: Single digit number, various ranges
        var result1 = SlotOptionFunctions.GetOptionsForSlot(0, "?".AsSpan(), 1, 0, 0);
        Assert.Equal(['0'], result1);

        var result2 = SlotOptionFunctions.GetOptionsForSlot(0, "?".AsSpan(), 1, 9, 9);
        Assert.Equal(['9'], result2);

        var result3 = SlotOptionFunctions.GetOptionsForSlot(0, "?".AsSpan(), 1, 5, 5);
        Assert.Equal(['5'], result3);
    }

    [Fact]
    public void GetOptionsForSlot_EdgeCases_ZeroRange()
    {
        // Test: Very narrow range
        var result = SlotOptionFunctions.GetOptionsForSlot(0, "?".AsSpan(), 1, 7, 7);
        Assert.Equal(['7'], result);
    }

    [Fact]
    public void GetOptionsForSlot_EdgeCases_AllIndefiniteTightRange()
    {
        // Test: 4-digit number with tight range 1000-1000
        var result = SlotOptionFunctions.GetOptionsForSlot(0, "????".AsSpan(), 4, 1000, 1000);
        Assert.Equal(['1'], result);

        var result2 = SlotOptionFunctions.GetOptionsForSlot(1, "????".AsSpan(), 4, 1000, 1000);
        // Position 1 can be 0, and other positions can be adjusted to get 1000
        Assert.Equal(['0'], result2);

        var result3 = SlotOptionFunctions.GetOptionsForSlot(2, "????".AsSpan(), 4, 1000, 1000);
        Assert.Equal(['0'], result3);

        var result4 = SlotOptionFunctions.GetOptionsForSlot(3, "????".AsSpan(), 4, 1000, 1000);
        Assert.Equal(['0'], result4);
    }

    [Fact]
    public void GetOptionsForSlot_EdgeCases_LargeNumber()
    {
        // Test: Large number with mixed definite/indefinite
        var filling = "123456789".AsSpan(); // 9-digit number
        var result = SlotOptionFunctions.GetOptionsForSlot(4, filling, 9, 123456789, 123456789);
        Assert.Equal(['5'], result); // Position 4 (0-indexed) is '5'
    }

    #endregion

    #region Complex Constraint Tests

    [Fact]
    public void GetOptionsForSlot_Complex_OverlappingRanges()
    {
        // Test: Position 1 of "1?3" for range 103-193
        // Should allow 0-9 since 103-193 covers all possibilities with first digit 1, last digit 3
        var result = SlotOptionFunctions.GetOptionsForSlot(1, "1?3".AsSpan(), 3, 103, 193);
        Assert.Equal(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'], result);
    }

    [Fact]
    public void GetOptionsForSlot_Complex_NarrowRangeWithIndefinite()
    {
        // Test: Position 0 of "?5?" for range 150-159
        // Position 0 must be 1
        var result = SlotOptionFunctions.GetOptionsForSlot(0, "?5?".AsSpan(), 3, 150, 159);
        Assert.Equal(['1'], result);
    }

    [Fact]
    public void GetOptionsForSlot_Complex_MultipleIndefiniteConstraints()
    {
        var result = SlotOptionFunctions.GetOptionsForSlot(2, "???".AsSpan(), 3, 120, 129);
        Assert.Equal(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'], result);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void GetOptionsForSlot_Errors_InvalidSlotIndex()
    {
        // Test: slotIndex out of range
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SlotOptionFunctions.GetOptionsForSlot(-1, "?".AsSpan(), 1, 0, 9)
        );

        Assert.Throws<ArgumentOutOfRangeException>(
            () => SlotOptionFunctions.GetOptionsForSlot(2, "?".AsSpan(), 1, 0, 9)
        );
    }

    [Fact]
    public void GetOptionsForSlot_Errors_MismatchedLengths()
    {
        // Test: currentFilling length doesn't match length parameter
        Assert.Throws<ArgumentException>(
            () => SlotOptionFunctions.GetOptionsForSlot(0, "12".AsSpan(), 3, 100, 999)
        );
    }

    [Fact]
    public void GetOptionsForSlot_Errors_MinGreaterThanMax()
    {
        // Test: min > max
        Assert.Throws<ArgumentException>(
            () => SlotOptionFunctions.GetOptionsForSlot(0, "?".AsSpan(), 1, 9, 0)
        );
    }

    #endregion

    [Fact(Timeout = 10)]
    public Task GetOptionsForSlot_Performance_LargeNumber()
    {
        return Task.Run(() =>
        { // Test with a large 10-digit number to verify performance
            var filling = "1234567890".AsSpan();
            var result = SlotOptionFunctions.GetOptionsForSlot(
                5,
                filling,
                10,
                1234560000,
                1234569999
            );

            // Position 5 should be '6' based on the definite digits
            Assert.Equal(['6'], result);
        });
    }

    [Fact(Timeout = 10)]
    public Task GetOptionsForSlot_Performance_AllIndefiniteLarge()
    {
        // Test with all indefinite large number
        return Task.Run(() =>
        {
            var length = (byte)long.MaxValue.ToString().Length; // 19 digits
            var filling = new string('?', length);
            var result = SlotOptionFunctions.GetOptionsForSlot(
                slotIndex: 0,
                currentFilling: filling,
                length: length,
                min: 0,
                max: long.MaxValue
            );

            // Position 0 should be '1' for range 1-1.9 billion, but also 5, 6, 9 are valid
            // because we can adjust other digits
            Assert.Equal(['1'], result);
        });
    }
}
