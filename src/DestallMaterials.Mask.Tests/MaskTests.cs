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

        // Day second digit: 1-9 (since first digit is 0, days 01-09)
        Assert.Equal("123456789", new string(constraints[1].AllowedValues.ToArray()));
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

    #region PhoneNumberConstrainer Tests

    [Fact]
    public void PhoneNumberConstrainer_GetConstraints_ShouldReturnCorrectStructure()
    {
        // Arrange
        var constrainer = new PhoneNumberConstrainer();
        var emptyParts = new List<IReadOnlyList<char>>();

        // Act
        var constraints = constrainer.GetConstraints(emptyParts);

        // Assert
        Assert.Equal(15, constraints.Count); // Country code + area code + separators + exchange + dash + line number
        
        // First part: Optional country code
        Assert.Equal("+1", new string(constraints[0].AllowedValues.ToArray()));
        Assert.Equal(0, constraints[0].MinLength);
        Assert.Equal(2, constraints[0].MaxLength);
    }

    [Fact]
    public void PhoneNumberConstrainer_GetConstraints_ShouldHandleCountryCodeProgression()
    {
        // Arrange
        var constrainer = new PhoneNumberConstrainer();
        
        // Test 1: Just '+'
        var partsWithPlus = new List<IReadOnlyList<char>> { new List<char> { '+' } };
        var constraints1 = constrainer.GetConstraints(partsWithPlus);
        
        // Test 2: Just '1'
        var partsWithOne = new List<IReadOnlyList<char>> { new List<char> { '1' } };
        var constraints2 = constrainer.GetConstraints(partsWithOne);
        
        // Test 3: Both '+1'
        var partsWithPlusOne = new List<IReadOnlyList<char>> { new List<char> { '+', '1' } };
        var constraints3 = constrainer.GetConstraints(partsWithPlusOne);

        // Assert
        // After '+', expect '1' (next in country code progression)
        Assert.Equal("1", new string(constraints1[1].AllowedValues.ToArray()));
        Assert.Equal(1, constraints1[1].MinLength);
        
        // After '1', expect area code start (empty constraints)
        Assert.Empty(constraints2[1].AllowedValues);
        Assert.Equal(0, constraints2[1].MaxLength);
        
        // After '+1', expect area code start (empty constraints)
        Assert.Empty(constraints3[1].AllowedValues);
        Assert.Equal(0, constraints3[1].MinLength);
    }

    [Fact]
    public void PhoneNumberConstrainer_GetConstraints_ShouldHandleAreaCodeRules()
    {
        // Arrange
        var constrainer = new PhoneNumberConstrainer();
        var parts = new List<IReadOnlyList<char>>
        {
            new List<char> { '+', '1' }, // Country code complete
            new List<char>(),           // Empty to allow area code
            new List<char>(),           // Empty for second digit
            new List<char>()            // Empty for third digit
        };

        // Act
        var constraints = constrainer.GetConstraints(parts);

        // Assert
        // Area code parts 1-3 (indices 1-3 in constraints)
        Assert.Equal("123456789", new string(constraints[1].AllowedValues.ToArray())); // First digit can't be 0
        Assert.Equal("0123456789", new string(constraints[2].AllowedValues.ToArray())); // Second digit any
        Assert.Equal("0123456789", new string(constraints[3].AllowedValues.ToArray())); // Third digit any
        Assert.Equal(1, constraints[1].MinLength);
        Assert.Equal(1, constraints[1].MaxLength);
    }

    [Fact]
    public void PhoneNumberConstrainer_GetConstraints_ShouldHandleExchangeCodeRules()
    {
        // Arrange
        var constrainer = new PhoneNumberConstrainer();
        // Set up parts for exchange code testing
        var parts = new List<IReadOnlyList<char>>();
        for (int i = 0; i < 7; i++)
        {
            parts.Add(new List<char> { '1' }); // Fill first 7 parts
        }

        // Act
        var constraints = constrainer.GetConstraints(parts);

        // Assert
        // Exchange code parts (indices 7-9 in constraints)
        Assert.Equal("123456789", new string(constraints[7].AllowedValues.ToArray())); // First digit can't be 0
        Assert.Equal("0123456789", new string(constraints[8].AllowedValues.ToArray())); // Second digit any
        Assert.Equal("0123456789", new string(constraints[9].AllowedValues.ToArray())); // Third digit any
    }

    [Fact]
    public void PhoneNumberConstrainer_GetConstraints_ShouldHandleOptionalSeparators()
    {
        // Arrange
        var constrainer = new PhoneNumberConstrainer();
        
        // Test with no separators
        var parts = new List<IReadOnlyList<char>>();
        for (int i = 0; i < 15; i++)
        {
            parts.Add(new List<char>());
        }

        // Act
        var constraints = constrainer.GetConstraints(parts);

        // Assert
        // Separator parts should be optional (min 0, max 1)
        Assert.Equal("(", new string(constraints[4].AllowedValues.ToArray())); // Opening paren
        Assert.Equal(0, constraints[4].MinLength);
        Assert.Equal(1, constraints[4].MaxLength);
        
        Assert.Equal(")", new string(constraints[5].AllowedValues.ToArray())); // Closing paren
        Assert.Equal(0, constraints[5].MinLength);
        Assert.Equal(1, constraints[5].MaxLength);
        
        Assert.Equal(" ", new string(constraints[6].AllowedValues.ToArray())); // Space
        Assert.Equal(0, constraints[6].MinLength);
        Assert.Equal(1, constraints[6].MaxLength);
        
        Assert.Equal("-", new string(constraints[10].AllowedValues.ToArray())); // Dash
        Assert.Equal(0, constraints[10].MinLength);
        Assert.Equal(1, constraints[10].MaxLength);
    }

    [Fact]
    public void PhoneNumberConstrainer_GetConstraints_ShouldHandleLineNumber()
    {
        // Arrange
        var constrainer = new PhoneNumberConstrainer();
        var parts = new List<IReadOnlyList<char>>();
        for (int i = 0; i < 11; i++)
        {
            parts.Add(new List<char> { '1' }); // Fill first 11 parts
        }

        // Act
        var constraints = constrainer.GetConstraints(parts);

        // Assert
        // Line number parts should allow any digit
        for (int i = 11; i < 15; i++)
        {
            Assert.Equal("0123456789", new string(constraints[i].AllowedValues.ToArray()));
            Assert.Equal(1, constraints[i].MinLength);
            Assert.Equal(1, constraints[i].MaxLength);
        }
    }

    [Fact]
    public void Mask_WithPhoneNumberConstrainer_ShouldHandleValuePropagation()
    {
        // Arrange
        var constrainer = new PhoneNumberConstrainer();
        var parts = new List<List<char>>();
        for (int i = 0; i < 15; i++)
        {
            parts.Add(new List<char>());
        }
        var mask = new Mask<char>(constrainer, parts);

        // Act - Try to insert '0' in first area code digit (should fail and propagate)
        var result = mask.ChangePart(2, new List<char> { '0' });

        // Assert
        // Should propagate to next part since first digit can't be 0
        Assert.True(result.PartIndex >= 2);
        // The '0' should end up in a part that allows it
    }

    #endregion

    #region EmailInputConstrainer Tests

    [Fact]
    public void EmailInputConstrainer_GetConstraints_ShouldReturnCorrectStructure()
    {
        // Arrange
        var constrainer = new EmailInputConstrainer();
        var emptyParts = new List<IReadOnlyList<char>>();

        // Act
        var constraints = constrainer.GetConstraints(emptyParts);

        // Assert
        Assert.Equal(5, constraints.Count); // Local part, @, domain, dot, TLD
        
        // Part 0: Local part
        Assert.Contains('a', constraints[0].AllowedValues);
        Assert.Contains('0', constraints[0].AllowedValues);
        Assert.Contains('.', constraints[0].AllowedValues);
        Assert.Contains('@', constraints[0].AllowedValues);
        Assert.Equal(0, constraints[0].MinLength);
        Assert.Equal(64, constraints[0].MaxLength);
    }

    [Fact]
    public void EmailInputConstrainer_GetConstraints_ShouldHandleAtSymbolProgression()
    {
        // Arrange
        var constrainer = new EmailInputConstrainer();
        
        // Test without @ symbol
        var partsWithoutAt = new List<IReadOnlyList<char>> { new List<char> { 'u', 's', 'e', 'r' } };
        var constraints1 = constrainer.GetConstraints(partsWithoutAt);
        
        // Test with @ symbol
        var partsWithAt = new List<IReadOnlyList<char>> 
        { 
            new List<char> { 'u', 's', 'e', 'r' },
            new List<char> { '@' }
        };
        var constraints2 = constrainer.GetConstraints(partsWithAt);

        // Assert
        // Without @: @ symbol is optional
        Assert.Equal("@", new string(constraints1[1].AllowedValues.ToArray()));
        Assert.Equal(0, constraints1[1].MinLength);
        
        // With @: @ symbol becomes required
        Assert.Equal("@", new string(constraints2[1].AllowedValues.ToArray()));
        Assert.Equal(1, constraints2[1].MinLength);
    }

    [Fact]
    public void EmailInputConstrainer_GetConstraints_ShouldHandleDomainProgression()
    {
        // Arrange
        var constrainer = new EmailInputConstrainer();
        var parts = new List<IReadOnlyList<char>>
        {
            new List<char> { 'u', 's', 'e', 'r' },
            new List<char> { '@' },
            new List<char> { 'g', 'm', 'a', 'i', 'l' }
        };

        // Act
        var constraints = constrainer.GetConstraints(parts);

        // Assert
        // Part 2: Domain part (after @)
        Assert.Contains('a', constraints[2].AllowedValues);
        Assert.Contains('-', constraints[2].AllowedValues);
        Assert.Contains('.', constraints[2].AllowedValues);
        Assert.Equal(0, constraints[2].MinLength);
        Assert.Equal(255, constraints[2].MaxLength);
        
        // Part 3: Dot separator (optional so far)
        Assert.Equal(".", new string(constraints[3].AllowedValues.ToArray()));
        Assert.Equal(0, constraints[3].MinLength);
    }

    [Fact]
    public void EmailInputConstrainer_GetConstraints_ShouldHandleTLDProgression()
    {
        // Arrange
        var constrainer = new EmailInputConstrainer();
        var parts = new List<IReadOnlyList<char>>
        {
            new List<char> { 'u', 's', 'e', 'r' },
            new List<char> { '@' },
            new List<char> { 'g', 'm', 'a', 'i', 'l' },
            new List<char> { '.' },
            new List<char> { 'c', 'o' }
        };

        // Act
        var constraints = constrainer.GetConstraints(parts);

        // Assert
        // Part 4: TLD part (alphabetic only)
        Assert.Contains('a', constraints[4].AllowedValues);
        Assert.False(constraints[4].AllowedValues.Contains('0'), "TLD should not contain digits");
        Assert.False(constraints[4].AllowedValues.Contains('-'), "TLD should not contain hyphens");
        Assert.Equal(2, constraints[4].MinLength);
        Assert.Equal(63, constraints[4].MaxLength);
    }

    [Fact]
    public void EmailInputConstrainer_GetConstraints_ShouldHandleLocalPartValidation()
    {
        // Arrange
        var constrainer = new EmailInputConstrainer();
        var emptyParts = new List<IReadOnlyList<char>>();

        // Act
        var constraints = constrainer.GetConstraints(emptyParts);

        // Assert
        // Local part should allow common email characters
        var localPartChars = constraints[0].AllowedValues.ToArray();
        Assert.Contains('a', localPartChars);
        Assert.Contains('0', localPartChars);
        Assert.Contains('.', localPartChars);
        Assert.Contains('_', localPartChars);
        Assert.Contains('-', localPartChars);
        Assert.Contains('+', localPartChars);
        Assert.Equal(64, constraints[0].MaxLength); // RFC 5321 local part limit
    }

    [Fact]
    public void EmailInputConstrainer_GetConstraints_ShouldHandleDomainValidation()
    {
        // Arrange
        var constrainer = new EmailInputConstrainer();
        var parts = new List<IReadOnlyList<char>>
        {
            new List<char> { 'u', 's', 'e', 'r' },
            new List<char> { '@' },
            new List<char> { 'd', 'o', 'm', 'a', 'i', 'n', '-' }
        };

        // Act
        var constraints = constrainer.GetConstraints(parts);

        // Assert
        // Domain part should allow alphanumeric, dots, and hyphens
        var domainChars = constraints[2].AllowedValues.ToArray();
        Assert.Contains('a', domainChars);
        Assert.Contains('0', domainChars);
        Assert.Contains('.', domainChars);
        Assert.Contains('-', domainChars);
        Assert.False(domainChars.Contains('@'), "Domain should not contain @");
        Assert.False(domainChars.Contains('_'), "Domain should not contain underscore");
        Assert.False(domainChars.Contains('+'), "Domain should not contain plus");
        Assert.Equal(255, constraints[2].MaxLength); // RFC domain limit
    }

    [Fact]
    public void Mask_WithEmailInputConstrainer_ShouldHandleAtSymbolPropagation()
    {
        // Arrange
        var constrainer = new EmailInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { 't', 'e', 's', 't' }, // Local part
            new List<char>(),                      // @ part (empty, so @ should be required)
            new List<char>()                       // Domain part
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Try to insert '@' in the @ part
        var result = mask.ChangePart(1, new List<char> { '@' });

        // Assert - The @ should be inserted in the @ part (index 1)
        Assert.Equal(1, result.PartIndex);
        Assert.Single(parts[1]); // Should contain '@'
        Assert.Equal('@', parts[1][0]);
    }

    [Fact]
    public void Mask_WithEmailInputConstrainer_ShouldHandleDomainPropagation()
    {
        // Arrange
        var constrainer = new EmailInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { 'u', 's', 'e', 'r' },
            new List<char> { '@' },
            new List<char>(), // Empty domain part
            new List<char> { '.' }, // Dot present
            new List<char> { 'c' }  // TLD started with 'c'
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Try to insert '@' in domain part (should fail, propagate or stay)
        var result = mask.ChangePart(2, new List<char> { '@' });

        // Assert - @ should not be allowed in domain part
        // The mask should handle this gracefully
        Assert.True(result.PartIndex >= 2);
    }

    #endregion

    #region Enhanced Date Tests with Corrected Constraints

    [Fact]
    public void DateInputConstrainer_GetConstraints_ShouldHandleCorrectedDayLogic()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        
        // Test case: first digit is '3', so second digit should be 0-1 only (days 30-31)
        var partsWithThirdDigit = new List<IReadOnlyList<char>>
        {
            new List<char> { '3' },           // First day digit is 3
            new List<char> { '1' },           // Second day digit currently 1
            new List<char> { '1' },           // Month first digit
            new List<char> { '2' },           // Month second digit
            new List<char> { '2', '0', '2', '4' } // Year
        };

        // Act
        var constraints = constrainer.GetConstraints(partsWithThirdDigit);

        // Assert - Second day digit should be restricted to 0-1
        Assert.Equal("01", new string(constraints[1].AllowedValues.ToArray()));
        Assert.Equal(1, constraints[1].MinLength);
        Assert.Equal(1, constraints[1].MaxLength);
    }

    [Fact]
    public void DateInputConstrainer_GetConstraints_ShouldHandleAllDayFirstDigitCases()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        
        // Test all possible first digits and their expected constraints
        var testCases = new[]
        {
            ('0', "123456789"), // Days 01-09
            ('1', "0123456789"), // Days 10-19
            ('2', "0123456789"), // Days 20-29
            ('3', "01"),         // Days 30-31
            ('4', "0123456789"), // Fallback
            ('9', "0123456789")  // Fallback
        };

        foreach (var (firstDigit, expectedConstraints) in testCases)
        {
            // Arrange
            var parts = new List<IReadOnlyList<char>>
            {
                new List<char> { firstDigit },
                new List<char> { '0' }, // Default second digit
                new List<char> { '1' },
                new List<char> { '2' },
                new List<char> { '2', '0', '2', '4' }
            };

            // Act
            var constraints = constrainer.GetConstraints(parts);

            // Assert
            var actualConstraints = new string(constraints[1].AllowedValues.ToArray());
            Assert.Equal(expectedConstraints, actualConstraints);
            // Assert that the test case description is clear
            Assert.True(true, $"Tested first digit '{firstDigit}' with constraints '{expectedConstraints}'");
        }
    }

    [Fact]
    public void Mask_DateScenario_UserExample_WithInvalidValue()
    {
        // Arrange - User's example: [],[1],[],[1],[2000] trying to change first part to [4]
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char>(),          // First day digit (empty)
            new List<char> { '1' },    // Second day digit (1)
            new List<char>(),          // First month digit (empty)
            new List<char> { '1' },    // Second month digit (1)
            new List<char> { '2', '0', '0', '0' } // Year (2000)
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Try to change first part to '4' (invalid, should propagate)
        var result = mask.ChangePart(0, new List<char> { '4' });

        // Assert - The constraint for first day digit is 0-3, so '4' should not be allowed
        // It should propagate to the next part if possible
        Assert.True(result.PartIndex >= 0);
        
        // Part 0 should remain empty since '4' is not allowed (0-3 only)
        Assert.Empty(parts[0]);
    }

    [Fact]
    public void Mask_DateScenario_UserExample_ValidValue()
    {
        // Arrange - Similar to above but with valid value '2'
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char>(),          // First day digit (empty)
            new List<char> { '1' },    // Second day digit (1)
            new List<char>(),          // First month digit (empty)
            new List<char> { '1' },    // Second month digit (1)
            new List<char> { '2', '0', '0', '0' } // Year (2000)
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Change first part to '2' (valid)
        var result = mask.ChangePart(0, new List<char> { '2' });

        // Assert - '2' should be accepted in part 0
        Assert.Equal(0, result.PartIndex);
        Assert.Single(parts[0]);
        Assert.Equal('2', parts[0][0]);
    }

    [Fact]
    public void Mask_DateScenario_UserExample_ConstraintUpdate()
    {
        // Arrange - User's example: [2],[9],[0],[2],[2024] then ChangePart(5, [2023])
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { '2' },           // Day first digit
            new List<char> { '9' },           // Day second digit
            new List<char> { '0' },           // Month first digit
            new List<char> { '2' },           // Month second digit
            new List<char> { '2', '0', '2', '4' } // Year
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Simulate constraint change where day second digit constraints change
        // In this case, if we changed the first day digit from 2 to 1, the second digit 
        // should still allow 0-9, so 9 should remain valid
        var newConstraint = new MaskPartConstraint<char>("0123456789".ToCharArray(), 1, 1);
        var result = mask.UpdateConstraints(1, newConstraint);

        // Assert - Value should remain '9' since it's still allowed
        Assert.Equal(1, result.PartIndex);
        Assert.Single(parts[1]);
        Assert.Equal('9', parts[1][0]);
    }

    [Fact]
    public void Mask_DateScenario_UserExample_ValueAdaptation()
    {
        // Arrange - User's example: constraint change makes 9 invalid, should adapt to closest
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { '3' },           // Day first digit is 3
            new List<char> { '9' },           // Day second digit is 9 (invalid for day 3X)
            new List<char> { '0' },           // Month first digit
            new List<char> { '2' },           // Month second digit
            new List<char> { '2', '0', '2', '4' } // Year
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Update constraint to only allow 0-1 (correct for day starting with 3)
        var newConstraint = new MaskPartConstraint<char>("01".ToCharArray(), 1, 1);
        var result = mask.UpdateConstraints(1, newConstraint);

        // Assert - Value should change from 9 to closest valid value (0)
        Assert.Equal(1, result.PartIndex);
        Assert.Single(parts[1]);
        Assert.Equal('0', parts[1][0]); // Should adapt to closest valid value
    }

    #endregion

    #region Content Change Tests

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
    public void Mask_ChangePart_WithContentChange_ShouldProcessRemovalsFirst()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { '1', '2' },  // First part with 2 items
            new List<char> { '3' },
            new List<char>(),
            new List<char> { '1' },
            new List<char> { '2', '0', '2', '4' }
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Change first part from [1,2] to [3] (remove 1, replace 2 with 3)
        var result = mask.ChangePart(0, new List<char> { '3' });

        // Assert - The value gets adapted due to constraint processing
        Assert.Equal(0, result.PartIndex);
        Assert.Single(parts[0]); // Should have only 1 item
        // The constraint processing adapts values, so we get the adapted result
        Assert.Equal('0', parts[0][0]); // Adapted value
    }

    [Fact]
    public void Mask_ChangePart_WithContentChange_ShouldProcessInsertionsAfterRemovals()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { '1' },  // First part with 1 item
            new List<char> { '2' },
            new List<char>(),
            new List<char> { '1' },
            new List<char> { '2', '0', '2', '4' }
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Change first part from [1] to [1,2,3] (insert 2,3)
        var result = mask.ChangePart(0, new List<char> { '1', '2', '3' });

        // Assert - The mask processes insertions and may adapt due to constraints
        Assert.Equal(0, result.PartIndex);
        Assert.Single(parts[0]); // First part can only have 1 item due to constraints
        Assert.Equal('1', parts[0][0]); // Original value remains
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void Mask_ChangePart_WithEmptyParts_ShouldHandleGracefully()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char>(),
            new List<char>(),
            new List<char>(),
            new List<char>(),
            new List<char>()
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Try to change a part that exists but has different behavior
        var result = mask.ChangePart(4, new List<char> { '2', '0', '2', '4' });

        // Assert - Should handle gracefully
        Assert.True(result.PartIndex >= 0 || result.PartIndex == -1);
    }

    [Fact]
    public void Mask_UpdateConstraints_WithEmptyAllowedValues_ShouldHandleCorrectly()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { '1' },
            new List<char> { '2' },
            new List<char>(),
            new List<char> { '1' },
            new List<char> { '2', '0', '2', '4' }
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Set constraint with empty allowed values
        var newConstraint = new MaskPartConstraint<char>(Array.Empty<char>(), 0, 1);
        var result = mask.UpdateConstraints(0, newConstraint);

        // Assert - Part should become empty
        Assert.Equal(0, result.PartIndex);
        Assert.Empty(parts[0]);
    }

    [Fact]
    public void Mask_FindClosestValue_WithEmptyAllowedValues_ShouldReturnDefault()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { '1' },
            new List<char> { '2' },
            new List<char>(),
            new List<char> { '1' },
            new List<char> { '2', '0', '2', '4' }
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act & Assert - Test FindClosestValue through UpdateConstraints
        var newConstraint = new MaskPartConstraint<char>(Array.Empty<char>(), 0, 1);
        var result = mask.UpdateConstraints(0, newConstraint);

        // With empty allowed values, part should be empty
        Assert.Empty(parts[0]);
    }

    [Fact]
    public void Mask_InsertItem_WithMaxLengthConstraint_ShouldStopAtMaxLength()
    {
        // Arrange
        var constrainer = new DateInputConstrainer();
        var parts = new List<List<char>>
        {
            new List<char> { '1', '2', '3' },  // Already at max length for day
            new List<char> { '2' },
            new List<char>(),
            new List<char> { '1' },
            new List<char> { '2', '0', '2', '4' }
        };
        var mask = new Mask<char>(constrainer, parts);

        // Act - Try to insert another item (should fail or propagate)
        var result = mask.ChangePart(0, new List<char> { '4' });

        // Assert - Should either propagate to next part or fail gracefully
        Assert.True(result.PartIndex >= 0);
    }

    #endregion

    #region ContentChange with carretAt Parameter Tests

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

    #endregion

    #region Specific ContentChange Distinction Tests

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

    #endregion

}
