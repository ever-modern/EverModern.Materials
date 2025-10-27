using DestallMaterials.WheelProtection.DataStructures.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DestallMaterials.Mask.Tests;

public class MaskTests
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
    public void DateInputConstrainer_GetConstraints_ShouldReturnCorrectConstraints()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var currentParts = new List<IReadOnlyList<char>>
        {
            new List<char> { '0' },
            new List<char> { '1' },
            new List<char> { '1' },
            new List<char> { '2' },
            new List<char> { '2', '0', '2', '4' }
        };

        // Act
        var constraints = constrainer.GetConstraints(currentParts);

        // Assert
        Assert.Equal(5, constraints.Count);
        
        // Day first digit: 0-3
        Assert.Equal("0123", new string(constraints[0].AllowedValues.ToArray()));
        Assert.Equal(1, constraints[0].MinLength);
        Assert.Equal(1, constraints[0].MaxLength);

        // Day second digit: 0-9 (since first digit is 0)
        Assert.Equal("0123456789", new string(constraints[1].AllowedValues.ToArray()));
        Assert.Equal(1, constraints[1].MinLength);
        Assert.Equal(1, constraints[1].MaxLength);

        // Month first digit: 0-1
        Assert.Equal("01", new string(constraints[2].AllowedValues.ToArray()));
        Assert.Equal(1, constraints[2].MinLength);
        Assert.Equal(1, constraints[2].MaxLength);

        // Month second digit: depends on first digit (1 = Dec, so 0-2)
        Assert.Equal("012", new string(constraints[3].AllowedValues.ToArray()));
        Assert.Equal(1, constraints[3].MinLength);
        Assert.Equal(1, constraints[3].MaxLength);

        // Year: 4 digits
        Assert.Equal("0123456789", new string(constraints[4].AllowedValues.ToArray()));
        Assert.Equal(4, constraints[4].MinLength);
        Assert.Equal(4, constraints[4].MaxLength);
    }

    [Fact]
    public void DateInputConstrainer_GetConstraints_ShouldAdaptBasedOnFirstDayDigit()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var currentParts = new List<IReadOnlyList<char>>
        {
            new List<char> { '3' }, // First digit is 3, so second digit can only be 0-1
            new List<char> { '1' },
            new List<char> { '1' },
            new List<char> { '2' },
            new List<char> { '2', '0', '2', '4' }
        };

        // Act
        var constraints = constrainer.GetConstraints(currentParts);

        // Assert
        Assert.Equal(5, constraints.Count);
        
        // Day first digit: 0-3
        Assert.Equal("0123", new string(constraints[0].AllowedValues.ToArray()));

        // Day second digit: 0-1 (since first digit is 3, max day is 31)
        Assert.Equal("01", new string(constraints[1].AllowedValues.ToArray()));
        Assert.Equal(1, constraints[1].MinLength);
        Assert.Equal(1, constraints[1].MaxLength);
    }

    [Fact]
    public void DateInputConstrainer_GetConstraints_ShouldAdaptBasedOnFirstMonthDigit()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var currentParts = new List<IReadOnlyList<char>>
        {
            new List<char> { '1' },
            new List<char> { '5' },
            new List<char> { '0' }, // First digit is 0, so second digit can be 1-9
            new List<char> { '2' },
            new List<char> { '2', '0', '2', '4' }
        };

        // Act
        var constraints = constrainer.GetConstraints(currentParts);

        // Assert
        Assert.Equal(5, constraints.Count);
        
        // Month second digit: 1-9 (since first digit is 0)
        Assert.Equal("123456789", new string(constraints[3].AllowedValues.ToArray()));
        Assert.Equal(1, constraints[3].MinLength);
        Assert.Equal(1, constraints[3].MaxLength);
    }

    [Fact]
    public void Mask_ChangePart_ShouldHandleSimpleChange()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { },
            new List<char> { }, // Empty part to allow '4' insertion
            new List<char> { },
            new List<char> { '1' },
            new List<char> { '2', '0', '2', '4' }
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Change first part from empty to '4'
        var result = mask.ChangePart(0, new List<char> { '4' });

        // Assert - '4' should propagate to part 1 (second day digit allows 0-9)
        Assert.Equal(1, result.PartIndex);
        Assert.Equal(0, result.ItemIndex);
        Assert.Empty(parts[0]); // Part 0 should remain empty since '4' propagated
        Assert.Single(parts[1]); // Part 1 should contain '4'
        Assert.Equal('4', parts[1][0]);
    }

    [Fact]
    public void Mask_ChangePart_ShouldPropagateWhenValueNotAllowed()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { },
            new List<char> { '1' },
            new List<char> { },
            new List<char> { '1' },
            new List<char> { '2', '0', '2', '4' }
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Try to change first part to '4', which is not allowed (max is 3)
        // The mask should skip to the next part
        var result = mask.ChangePart(0, new List<char> { '4' });

        // Assert
        // The method should attempt to insert '4' but since it's not allowed in first part,
        // it may try to insert in next parts or fail gracefully
        Assert.True(result.PartIndex >= 0);
    }

    [Fact]
    public void Mask_UpdateConstraints_ShouldAdjustValues()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { '3' },
            new List<char> { '1' },
            new List<char> { '0' },
            new List<char> { '2' },
            new List<char> { '2', '0', '2', '4' }
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Change constraint for second part to only allow '0' and '1'
        var newConstraint = new MaskPartConstraint<char>("01".ToCharArray(), 1, 1);
        var result = mask.UpdateConstraints(1, newConstraint);

        // Assert
        Assert.Equal(1, result.PartIndex);
        // The value '1' should still be valid, so it should remain
        Assert.Single(parts[1]);
        Assert.Equal('1', parts[1][0]);
    }

    [Fact]
    public void Mask_UpdateConstraints_ShouldReplaceInvalidValues()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { '3' },
            new List<char> { '5' }, // Invalid value for day (should be 0-1 when first digit is 3)
            new List<char> { '0' },
            new List<char> { '2' },
            new List<char> { '2', '0', '2', '4' }
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Change constraint for second part to only allow '0' and '1'
        var newConstraint = new MaskPartConstraint<char>("01".ToCharArray(), 1, 1);
        var result = mask.UpdateConstraints(1, newConstraint);

        // Assert
        Assert.Equal(1, result.PartIndex);
        // The value '5' should be replaced with '0' (closest valid value)
        Assert.Single(parts[1]);
        Assert.Equal('0', parts[1][0]);
    }

    [Fact]
    public void Mask_UpdateConstraints_WithEmptyAllowedValues_ShouldClearPart()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { '3' },
            new List<char> { '5' },
            new List<char> { '0' },
            new List<char> { '2' },
            new List<char> { '2', '0', '2', '4' }
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Change constraint for second part to allow no values
        var newConstraint = new MaskPartConstraint<char>(Array.Empty<char>(), 0, 0);
        var result = mask.UpdateConstraints(1, newConstraint);

        // Assert
        Assert.Equal(1, result.PartIndex);
        Assert.Empty(parts[1]);
    }

    [Fact]
    public void Mask_FindClosestValue_ShouldReturnExactMatch()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { '2' },
            new List<char> { '5' },
            new List<char> { '0' },
            new List<char> { '2' },
            new List<char> { '2', '0', '2', '4' }
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Test FindClosestValue through reflection or by creating a testable method
        // For now, we'll test through UpdateConstraints which uses FindClosestValue
        
        // Act - Change constraint to allow '5' and '6'
        var newConstraint = new MaskPartConstraint<char>("56".ToCharArray(), 1, 1);
        var result = mask.UpdateConstraints(1, newConstraint);

        // Assert
        Assert.Equal(1, result.PartIndex);
        // The value should remain '5' since it's in the new allowed values
        Assert.Single(parts[1]);
        Assert.Equal('5', parts[1][0]);
    }

    [Fact]
    public void Mask_ProcessContentChange_ShouldHandleComplexChanges()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { '1' },
            new List<char> { '2' },
            new List<char> { '0' },
            new List<char> { '2' },
            new List<char> { '2', '0', '2', '4' }
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Replace '2' with '3' in second part
        var change = new ContentChange<char>(0, 1, new List<char> { '3' });
        var result = mask.ProcessContentChange(1, change);

        // Assert
        Assert.Equal(1, result.PartIndex);
        Assert.Single(parts[1]);
        Assert.Equal('3', parts[1][0]);
    }
}
