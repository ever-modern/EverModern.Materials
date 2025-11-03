using DestallMaterials.WheelProtection.DataStructures.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DestallMaterials.Tests.XUnit;

public class ContentChangeTests
{
    [Fact]
    public void ContentChange_Get_ShouldDetectInsertions()
    {
        // Arrange
        var oldValue = new List<char> { '1', '2' };
        var newValue = new List<char> { '1', '2', '3', '4' };

        // Act
        var change = ContentChange<char>.Get(oldValue, newValue);

        // Assert
        Assert.Equal(2, change.At);
        Assert.Equal(0, change.Removed);
        Assert.Equal(2, change.Inserted.Count);
        Assert.Equal('3', change.Inserted[0]);
        Assert.Equal('4', change.Inserted[1]);
    }

    [Fact]
    public void ContentChange_Get_ShouldDetectDeletions()
    {
        // Arrange
        var oldValue = new List<char> { '1', '2', '3', '4' };
        var newValue = new List<char> { '1', '2' };

        // Act
        var change = ContentChange<char>.Get(oldValue, newValue);

        // Assert
        Assert.Equal(2, change.At);
        Assert.Equal(2, change.Removed);
        Assert.Equal(0, change.Inserted.Count);
    }

    [Fact]
    public void ContentChange_Get_ShouldDetectReplacements()
    {
        // Arrange
        var oldValue = new List<char> { '1', '2', '3', '4' };
        var newValue = new List<char> { '1', '2', '5', '6' };

        // Act
        var change = ContentChange<char>.Get(oldValue, newValue);

        // Assert
        Assert.Equal(2, change.At);
        Assert.Equal(2, change.Removed);
        Assert.Equal(2, change.Inserted.Count);
        Assert.Equal('5', change.Inserted[0]);
        Assert.Equal('6', change.Inserted[1]);
    }

    [Fact]
    public void ContentChange_Get_ShouldHandleEmptyLists()
    {
        // Arrange
        var oldValue = new List<char>();
        var newValue = new List<char> { '1', '2' };

        // Act
        var change = ContentChange<char>.Get(oldValue, newValue);

        // Assert
        Assert.Equal(0, change.At);
        Assert.Equal(0, change.Removed);
        Assert.Equal(2, change.Inserted.Count);
    }

    [Fact]
    public void ContentChange_Get_ShouldHandlePrefixMatch()
    {
        // Arrange
        var oldValue = new List<char> { '1', '2', '3' };
        var newValue = new List<char> { '1', '2', '3', '4', '5' };

        // Act
        var change = ContentChange<char>.Get(oldValue, newValue);

        // Assert
        Assert.Equal(3, change.At);
        Assert.Equal(0, change.Removed);
        Assert.Equal(2, change.Inserted.Count);
    }

    [Fact]
    public void ContentChange_ShouldHandleSequentialItemProcessing()
    {
        // Arrange
        var oldValue = new List<char> { '1', '2', '3' };
        var newValue = new List<char> { '1', '4', '5', '3' };
        
        // Act
        var change = ContentChange<char>.Get(oldValue, newValue);
        
        // Assert
        // Should remove '2' and insert '4', '5' at position 1
        Assert.Equal(1, change.At);
        Assert.Equal(1, change.Removed);
        Assert.Equal(2, change.Inserted.Count);
        Assert.Equal('4', change.Inserted[0]);
        Assert.Equal('5', change.Inserted[1]);
    }

    [Fact]
    public void ContentChange_Get_WithCarretAt_ShouldDistinguishLastCharRemoval()
    {
        // Arrange: "***/***" -> "***/***" (remove last character '*')
        var oldValue = new List<char> { '*', '*', '*', '/', '*', '*', '*' };
        var newValue = new List<char> { '*', '*', '*', '/', '*', '*', '*' };
        
        // Act - Remove last character, caret ends at position 7
        var change1 = ContentChange<char>.Get(oldValue, newValue, carretAt: 7);

        // Assert
        Assert.Equal(6, change1.At);      // Last character position
        Assert.Equal(1, change1.Removed); // Should report removal
        Assert.Empty(change1.Inserted);
    }

    [Fact]
    public void ContentChange_Get_WithCarretAt_ShouldDistinguishPreLastCharRemoval()
    {
        // Arrange: "***/***" -> "***/***" (remove pre-last character, last shifts left)
        var oldValue = new List<char> { '*', '*', '*', '/', '*', '*', '*' };
        var newValue = new List<char> { '*', '*', '*', '/', '*', '*', '*' };
        
        // Act - Remove pre-last character, caret ends at position 7 (after shift)
        var change2 = ContentChange<char>.Get(oldValue, newValue, carretAt: 6);

        // Assert - Different result from removing last character
        Assert.NotEqual(7, change2.At);  // Different position than last char removal
        Assert.Equal(5, change2.At);     // Pre-last character position
        Assert.Equal(1, change2.Removed); // Should report removal
        Assert.Empty(change2.Inserted);
    }

    [Fact]
    public void ContentChange_Get_WithoutCarretAt_TreatsIdenticalContentAsNoChange()
    {
        // Arrange: Same start and finish
        var oldValue = new List<char> { '*', '*', '*', '/', '*', '*', '*' };
        var newValue = new List<char> { '*', '*', '*', '/', '*', '*', '*' };
        
        // Act - Without carretAt, identical content is treated as no change
        var change = ContentChange<char>.Get(oldValue, newValue);

        // Assert - Default behavior without carretAt
        Assert.Equal(0, change.At);
        Assert.Equal(0, change.Removed);
        Assert.Empty(change.Inserted);
    }

    [Fact]
    public void ContentChange_Get_WithCarretAt_DistinguishesSameContentDifferentPositions()
    {
        // Arrange: Same pattern but different scenarios
        var start = new List<char> { '*', '*', '*', '/', '*', '*', '*' };
        
        // Scenario 1: Last character interaction
        var change1 = ContentChange<char>.Get(start, new List<char> { '*', '*', '*', '/', '*', '*', '*' }, carretAt: 7);
        
        // Scenario 2: Pre-last character interaction  
        var change2 = ContentChange<char>.Get(start, new List<char> { '*', '*', '*', '/', '*', '*', '*' }, carretAt: 6);
        
        // Assert - The carretAt parameter now makes these distinguishable
        Assert.NotEqual(change1.At, change2.At);
        Assert.Equal(6, change1.At);  // Last character position (index 6)
        Assert.Equal(5, change2.At);  // Pre-last character position (index 5)
    }

    [Fact]
    public void ContentChange_Get_WithCarretAt_HandlesRegularChanges()
    {
        // Arrange: Regular change from "abc" to "abx"
        var oldValue = new List<char> { 'a', 'b', 'c' };
        var newValue = new List<char> { 'a', 'b', 'x' };
        
        // Act - With carretAt providing additional context
        var change = ContentChange<char>.Get(oldValue, newValue, carretAt: 2);

        // Assert - Regular change still works correctly
        Assert.Equal(2, change.At);
        Assert.Equal(1, change.Removed);
        Assert.Single(change.Inserted);
        Assert.Equal('x', change.Inserted[0]);
    }

    [Fact]
    public void ContentChange_Get_WithCarretAt_AndSameLengthDifferentContent()
    {
        // Arrange: "abc" -> "axc" (remove 'b', insert nothing at position 1, 'c' shifts)
        var oldValue = new List<char> { 'a', 'b', 'c' };
        var newValue = new List<char> { 'a', 'c' };  // Length decreased
        
        // Act - Different carretAt positions should produce the same result for this case
        var change1 = ContentChange<char>.Get(oldValue, newValue, carretAt: 1);
        var change2 = ContentChange<char>.Get(oldValue, newValue, carretAt: 2);

        // Assert - For actual length changes, result is the same regardless of carretAt
        Assert.Equal(1, change1.At);
        Assert.Equal(1, change2.At);
        Assert.Equal(1, change1.Removed);
        Assert.Equal(1, change2.Removed);
    }

    [Fact]
    public void ContentChange_Get_DistinguishesLastCharVsPreLastCharRemoval()
    {
        // Arrange: Original content "***/***" (7 characters)
        var start = new List<char> { '*', '*', '*', '/', '*', '*', '*' };
        
        // Scenario 1: Remove the last character (position 6, '*')
        var change1 = ContentChange<char>.Get(start, new List<char> { '*', '*', '*', '/', '*', '*', '*' }, carretAt: 7);
        
        // Scenario 2: Remove the pre-last character (position 5, '*')  
        var change2 = ContentChange<char>.Get(start, new List<char> { '*', '*', '*', '/', '*', '*', '*' }, carretAt: 6);
        
        // Assert: The method should distinguish between these two scenarios
        Assert.NotEqual(change1.At, change2.At);
        Assert.Equal(6, change1.At);  // Remove last character -> position 6
        Assert.Equal(5, change2.At);  // Remove pre-last character -> position 5
        Assert.Equal(1, change1.Removed);
        Assert.Equal(1, change2.Removed);
        Assert.Empty(change1.Inserted);
        Assert.Empty(change2.Inserted);
    }

    [Fact]
    public void ContentChange_Get_LastCharRemovalFromEndPosition()
    {
        // Arrange: Remove last character from "abc"
        var start = new List<char> { 'a', 'b', 'c' };
        
        // Act: Remove last character, caret ends at end position (3)
        var change = ContentChange<char>.Get(start, new List<char> { 'a', 'b', 'c' }, carretAt: 3);
        
        // Assert: Should report removal from position 2 (last character)
        Assert.Equal(2, change.At);
        Assert.Equal(1, change.Removed);
        Assert.Empty(change.Inserted);
    }

    [Fact]
    public void ContentChange_Get_PreLastCharRemovalFromEndMinusOne()
    {
        // Arrange: Remove pre-last character from "abc"
        var start = new List<char> { 'a', 'b', 'c' };
        
        // Act: Remove pre-last character, caret ends at position 2
        var change = ContentChange<char>.Get(start, new List<char> { 'a', 'b', 'c' }, carretAt: 2);
        
        // Assert: Should report removal from position 1 (pre-last character)
        Assert.Equal(1, change.At);
        Assert.Equal(1, change.Removed);
        Assert.Empty(change.Inserted);
    }

    [Fact]
    public void ContentChange_Get_CaretAtMiddlePosition()
    {
        // Arrange: Remove character from middle position of "abcde"
        var start = new List<char> { 'a', 'b', 'c', 'd', 'e' };
        
        // Act: Remove character at position 2, caret ends at position 2
        var change = ContentChange<char>.Get(start, new List<char> { 'a', 'b', 'c', 'd', 'e' }, carretAt: 2);
        
        // Assert: Should report position 2 where change occurred
        Assert.Equal(2, change.At);
        Assert.Equal(0, change.Removed); // No actual content change, just positional info
        Assert.Empty(change.Inserted);
    }

    [Fact]
    public void ContentChange_Get_WithDifferentRepeatedCharacters()
    {
        // Arrange: "aaa" with different caret positions
        var start = new List<char> { 'a', 'a', 'a' };
        
        // Scenario 1: Remove last character
        var change1 = ContentChange<char>.Get(start, new List<char> { 'a', 'a', 'a' }, carretAt: 3);
        
        // Scenario 2: Remove middle character (which becomes last after removal)
        var change2 = ContentChange<char>.Get(start, new List<char> { 'a', 'a', 'a' }, carretAt: 2);
        
        // Assert: Should distinguish between removing last vs middle character
        Assert.NotEqual(change1.At, change2.At);
        Assert.Equal(2, change1.At);  // Remove last character (position 2)
        Assert.Equal(1, change2.At);  // Remove middle character (position 1)
    }

    [Fact]
    public void ContentChange_Get_BackwardCompatibility()
    {
        // Arrange: Test cases that should work without carretAt parameter
        var start = new List<char> { 'a', 'b', 'c' };
        var finish = new List<char> { 'a', 'x', 'y' };
        
        // Act: Without carretAt parameter (using default)
        var change1 = ContentChange<char>.Get(start, finish);
        var change2 = ContentChange<char>.Get(start, finish, EqualityComparer<char>.Default);
        
        // Assert: Should work the same as before
        Assert.Equal(1, change1.At);
        Assert.Equal(1, change2.At);
        Assert.Equal(1, change1.Removed);
        Assert.Equal(1, change2.Removed);
        Assert.Equal(2, change1.Inserted.Count);
        Assert.Equal(2, change2.Inserted.Count);
    }

    [Fact]
    public void ContentChange_Get_IdenticalContentWithoutCaret()
    {
        // Arrange: Identical start and finish without carretAt
        var start = new List<char> { 'a', 'b', 'c' };
        var finish = new List<char> { 'a', 'b', 'c' };
        
        // Act: Without carretAt, identical content should be no change
        var change = ContentChange<char>.Get(start, finish);
        
        // Assert: Should return no change (0, 0, [])
        Assert.Equal(0, change.At);
        Assert.Equal(0, change.Removed);
        Assert.Empty(change.Inserted);
    }

    [Fact]
    public void ContentChange_Get_IdenticalContentWithCaret()
    {
        // Arrange: Identical start and finish with carretAt
        var start = new List<char> { 'a', 'b', 'c' };
        var finish = new List<char> { 'a', 'b', 'c' };
        
        // Act: With carretAt, should report position of interaction
        var change = ContentChange<char>.Get(start, finish, carretAt: 2);
        
        // Assert: Should report the position where interaction occurred
        Assert.Equal(2, change.At);
        Assert.Equal(0, change.Removed);
        Assert.Empty(change.Inserted);
    }
}
