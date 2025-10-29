namespace DestallMaterials.WheelProtection.DataStructures.Text;

public class Mask<TSymbol>(
    ISlotConstraintsSource<TSymbol> constraintsSource,
    IReadOnlyList<TSymbol?> initialSlots,
    IEqualityComparer<TSymbol?> equalityComparer
) : IMask<TSymbol>
    where TSymbol : struct
{
    public IReadOnlyList<TSymbol?> Slots => _slots;

    IReadOnlyList<SlotConstraint<TSymbol>> Constraints => constraintsSource.GetConstraints(_slots);

    readonly TSymbol?[] _slots = [.. initialSlots];

    public int AcceptChange(ContentChange<TSymbol> contentChange)
    {
        var (at, removed, inserted) = contentChange;

        if (at >= _slots.Length)
        {
            throw new InvalidOperationException("Can't change beyond slots count.");
        }

        var options = Constraints;
        var currentSlotIndex = at;
        for (int i = 0; i < removed && currentSlotIndex >= 0; i++)
        {
            var slotOptions = options[i].Options;
            currentSlotIndex = at - i;
            _slots[currentSlotIndex] = slotOptions.Count == 1 ? slotOptions[0] : null;
            currentSlotIndex--;
            AutosetAll();
        }

        if (currentSlotIndex < 0)
        {
            currentSlotIndex = 0;
        }

        options = Constraints;

        for (int i = 0; i < inserted.Count || currentSlotIndex == _slots.Length; )
        {
            var slotOptions = options[currentSlotIndex].Options;
            var insertedValue = inserted[i];

            if (slotOptions.Count == 1)
            {
                _slots[currentSlotIndex] = slotOptions[0];
            }
            else if (slotOptions.Count == 0)
            {
                _slots[currentSlotIndex] = null;
            }
            else
            {
                _slots[currentSlotIndex] = slotOptions.Contains(insertedValue)
                    ? insertedValue
                    : _slots[currentSlotIndex];

                currentSlotIndex++;

                AutosetAll();
            }
        }

        return currentSlotIndex; // carret position is +1
    }

    bool AutosetAll()
    {
        var changed = false;
        var currentConstraints = Constraints;
        for (int i = 0; i < _slots.Length; i++)
        {
            var slotValue = _slots[i];
            var options = currentConstraints[i].Options;
            var newSlotValue = slotValue;
            if (options.Count == 1)
            {
                newSlotValue = options[0];
            }
            else if (options.Count == 0)
            {
                newSlotValue= null;
            }
            else if (slotValue is not null && options.Contains(slotValue) is false)
            {
                newSlotValue = options[^1];                
            }

            _slots[i] = newSlotValue;

            changed = changed || (equalityComparer.Equals(newSlotValue, slotValue) is false);
        }

        return changed && AutosetAll();
    }
}

#region Garbage

[Obsolete]
public class GarbageMask<T>(
    IConstrainer<T> constrainer,
    IReadOnlyList<List<T>> parts,
    IEqualityComparer<T> equalityComparer
)
{
    public GarbageMask(IConstrainer<T> textConstrainer, IReadOnlyList<List<T>> parts)
        : this(textConstrainer, parts, EqualityComparer<T>.Default) { }

#if DEBUG
    public List<string> Diagnostics = [];
#endif

    void Log(string message)
    {
#if DEBUG
        Diagnostics.Add(message);
#endif
    }

    /// <summary>
    /// Makes projection of mask onto a new string value, based on the difference with the previous string value.
    /// </summary>
    /// <param name="partIndex">Index of the part to change</param>
    /// <param name="newValue">New value for the part</param>
    /// <returns>Tuple containing (PartIndex, ItemIndex) of where the change propagated to</returns>
    public (int PartIndex, int ItemIndex) ChangePart(int partIndex, IReadOnlyList<T> newValue)
    {
        var (at, deleted, inserted) = GetChange(parts[partIndex], newValue);
        Log($"ChangePart: part={partIndex}, at={at}, deleted={deleted}, inserted={inserted.Count}");

        // Process removals first
        for (int i = 0; i < deleted; i++)
        {
            var deleteIndex = at;
            DeleteItem(partIndex, deleteIndex);

            // After deletion, the parts array changes, so we need to get fresh constraints
            var constraints = constrainer.GetConstraints(parts);
            if (partIndex < constraints.Count)
            {
                var constraint = constraints[partIndex];
                DecideSingleOptions(parts[partIndex], constraint);
            }
        }

        // Process insertions
        for (int i = 0; i < inserted.Count; i++)
        {
            var insertIndex = at + i;
            var result = InsertItem(insertIndex, inserted[i]);
            if (result == (-1, -1))
            {
                // Couldn't insert at this position, try next position
                continue;
            }
            return result;
        }

        // Return the position where we stopped
        return (partIndex, at);
    }

    static ContentChange<T> GetChange(IReadOnlyList<T> oldValue, IReadOnlyList<T> newValue) =>
        ContentChange<T>.Get(oldValue, newValue);

    void DeleteItem(int partIndex, int itemIndex)
    {
        if (partIndex < 0 || partIndex >= parts.Count)
            return;

        if (itemIndex < 0 || itemIndex >= parts[partIndex].Count)
            return;

        Log($"Deleting item at part {partIndex}, index {itemIndex}");
        parts[partIndex].RemoveAt(itemIndex);
    }

    /// <summary>
    /// Try to insert an item at the specified position, considering constraints
    /// </summary>
    /// <param name="position">Position to insert</param>
    /// <param name="item">Item to insert</param>
    /// <returns>Tuple (PartIndex, ItemIndex) where insertion happened, or (-1, -1) if failed</returns>
    (int PartIndex, int ItemIndex) InsertItem(int position, T item)
    {
        var currentPosition = position;

        while (currentPosition < parts.Count)
        {
            var constraints = constrainer.GetConstraints(parts);
            if (currentPosition >= constraints.Count)
                break;

            var constraint = constraints[currentPosition];

            // Check if the item is allowed
            if (!constraint.AllowedValues.Contains(item, equalityComparer))
            {
                Log($"Item {item} not allowed at position {currentPosition}, trying next");
                currentPosition++;
                continue;
            }

            // Check if this part has reached its max length
            if (parts[currentPosition].Count >= constraint.MaxLength)
            {
                Log(
                    $"Part {currentPosition} reached max length {constraint.MaxLength}, trying next"
                );
                currentPosition++;
                continue;
            }

            // Add the item to this part
            parts[currentPosition].Add(item);
            Log($"Added {item} to part {currentPosition}");

            return (currentPosition, parts[currentPosition].Count - 1);
        }

        return (-1, -1);
    }

    /// <summary>
    /// Update existing values when constraints change
    /// </summary>
    /// <param name="partIndex">Index of the part whose constraints changed</param>
    /// <param name="newConstraints">New constraints for this part</param>
    /// <returns>Tuple (PartIndex, ItemIndex) of where the adjustment propagated to</returns>
    public (int PartIndex, int ItemIndex) UpdateConstraints(
        int partIndex,
        MaskPartConstraint<T> newConstraints
    )
    {
        Log($"Updating constraints for part {partIndex}");

        if (partIndex < 0 || partIndex >= parts.Count)
            return (-1, -1);

        var part = parts[partIndex];
        var currentValues = new List<T>(part);

        // Clear the part
        part.Clear();

        // Try to remap each value to the new constraints
        foreach (var value in currentValues)
        {
            var valueIndexInOld = -1;
            var constraints = constrainer.GetConstraints(parts);
            var allowedValuesList =
                partIndex < constraints.Count ? constraints[partIndex].AllowedValues : null;

            if (allowedValuesList != null)
            {
                // Find index of value in old constraint's allowed values
                for (int i = 0; i < allowedValuesList.Count; i++)
                {
                    if (equalityComparer.Equals(allowedValuesList[i], value))
                    {
                        valueIndexInOld = i;
                        break;
                    }
                }
            }

            // Find the closest value in the new constraints
            var newValue = FindClosestValue(value, newConstraints.AllowedValues, valueIndexInOld);
            if (newValue != null && newConstraints.AllowedValues.Count > 0)
            {
                part.Add(newValue);
            }
            else if (newConstraints.MinLength > 0 && newConstraints.AllowedValues.Count > 0)
            {
                // No valid value found, try to insert at next part
                var firstAllowed = newConstraints.AllowedValues.FirstOrDefault();
                if (firstAllowed is not null)
                {
                    var result = InsertItem(partIndex + 1, firstAllowed);
                    if (result != (-1, -1))
                        return result;
                }
            }
        }

        // Ensure min length is satisfied (only if there are allowed values)
        while (part.Count < newConstraints.MinLength && newConstraints.AllowedValues.Count > 0)
        {
            part.Add(newConstraints.AllowedValues[0]);
        }

        return (partIndex, part.Count > 0 ? part.Count - 1 : 0);
    }

    T? FindClosestValue(T originalValue, IReadOnlyList<T> allowedValues, int originalIndex)
    {
        if (allowedValues.Count == 0)
            return default(T);

        // Try exact match first
        if (allowedValues.Contains(originalValue, equalityComparer))
            return originalValue;

        // If we had an original index, try to find closest by index
        if (originalIndex >= 0 && originalIndex < allowedValues.Count)
        {
            return allowedValues[originalIndex];
        }

        // Otherwise, return the first allowed value
        return allowedValues[0];
    }

    /// <summary>
    /// Process ContentChange and adjust all affected parts
    /// </summary>
    /// <param name="partIndex">Index of the part</param>
    /// <param name="change">ContentChange describing what changed</param>
    /// <returns>Tuple (PartIndex, ItemIndex) of where the change propagated to</returns>
    public (int PartIndex, int ItemIndex) ProcessContentChange(
        int partIndex,
        ContentChange<T> change
    )
    {
        Log(
            $"Processing ContentChange: part={partIndex}, at={change.At}, removed={change.Removed}, inserted={change.Inserted.Count}"
        );

        // Process removals first
        for (int i = 0; i < change.Removed; i++)
        {
            DeleteItem(partIndex, change.At);
        }

        // Process insertions
        for (int i = 0; i < change.Inserted.Count; i++)
        {
            var item = change.Inserted[i];
            var insertPosition = change.At + i;
            var result = InsertItem(insertPosition, item);
            if (result != (-1, -1))
                return result;
        }

        return (partIndex, change.At);
    }

    /// <summary>
    /// Paste values into spots for which there can be only one value.
    /// </summary>
    /// <param name="symbols"></param>
    /// <param name="constraints"></param>
    public static void DecideSingleOptions(List<T> symbols, MaskPartConstraint<T> constraints)
    {
        // If the part is empty and has min length > 0, add the first allowed value
        if (symbols.Count < constraints.MinLength && constraints.AllowedValues.Count > 0)
        {
            symbols.Add(constraints.AllowedValues[0]);
        }
    }
}

/// <summary>
///
/// </summary>
/// <param name="AllowedValues"></param>
/// <param name="MinLength"></param>
/// <param name="MaxLength"></param>
public record struct MaskPartConstraint<T>(
    IReadOnlyList<T> AllowedValues,
    int MinLength,
    int MaxLength
);

/// <summary>
/// Returns constraints on text values, based on already present text content.
/// </summary>
public interface IConstrainer<T>
{
    public IReadOnlyList<MaskPartConstraint<T>> GetConstraints(
        IReadOnlyList<IReadOnlyList<T>> currentTextParts
    );
}

public class PhoneNumberConstrainer : IConstrainer<char>
{
    public IReadOnlyList<MaskPartConstraint<char>> GetConstraints(
        IReadOnlyList<IReadOnlyList<char>> currentSymbols
    )
    {
        // Phone number format: +1 (XXX) XXX-XXXX or XXX-XXX-XXXX
        // Fixed structure with 14 parts:
        // Part 0: Country code indicator (+ or 1 or digits)
        // Parts 1-3: Area code (XXX)
        // Parts 4-6: Separators '(', ')', ' '
        // Parts 7-9: Exchange code (XXX)
        // Part 10: '-'
        // Parts 11-14: Line number (XXXX)

        var constraints = new List<MaskPartConstraint<char>>();

        // Part 0: Country code or start of area code
        if (currentSymbols.Count > 0 && currentSymbols[0].Count > 0)
        {
            var firstChar = currentSymbols[0][0];
            if (firstChar == '+')
            {
                constraints.Add(new MaskPartConstraint<char>("1".ToCharArray(), 1, 1));
            }
            else if (firstChar == '1')
            {
                constraints.Add(new MaskPartConstraint<char>(Array.Empty<char>(), 0, 0));
            }
            else
            {
                // Allow digits for area code start
                constraints.Add(new MaskPartConstraint<char>("123456789".ToCharArray(), 1, 1));
            }
        }
        else
        {
            constraints.Add(new MaskPartConstraint<char>("+123456789".ToCharArray(), 0, 1));
        }

        // Parts 1-3: Area code (XXX)
        for (int i = 0; i < 3; i++)
        {
            var allowedValues = i == 0 ? "123456789" : "0123456789";
            constraints.Add(new MaskPartConstraint<char>(allowedValues.ToCharArray(), 1, 1));
        }

        // Parts 4-6: Separators
        constraints.Add(new MaskPartConstraint<char>("(".ToCharArray(), 0, 1));
        constraints.Add(new MaskPartConstraint<char>(")".ToCharArray(), 0, 1));
        constraints.Add(new MaskPartConstraint<char>(" ".ToCharArray(), 0, 1));

        // Parts 7-9: Exchange code
        for (int i = 0; i < 3; i++)
        {
            var allowedValues = i == 0 ? "123456789" : "0123456789";
            constraints.Add(new MaskPartConstraint<char>(allowedValues.ToCharArray(), 1, 1));
        }

        // Part 10: Dash
        constraints.Add(new MaskPartConstraint<char>("-".ToCharArray(), 0, 1));

        // Parts 11-14: Line number
        for (int i = 0; i < 4; i++)
        {
            constraints.Add(new MaskPartConstraint<char>("0123456789".ToCharArray(), 1, 1));
        }

        return constraints;
    }
}

/// <summary>
/// Email input constrainer for testing Mask functionality
/// </summary>
public class EmailInputConstrainer : IConstrainer<char>
{
    public IReadOnlyList<MaskPartConstraint<char>> GetConstraints(
        IReadOnlyList<IReadOnlyList<char>> currentTextParts
    )
    {
        // Email format: local@domain.tld
        // Progressive validation with 5 parts:
        // Part 0: Local part (before @)
        // Part 1: @ symbol
        // Part 2: Domain part (before .)
        // Part 3: Dot separator
        // Part 4: TLD part

        var constraints = new List<MaskPartConstraint<char>>();

        // Analyze current state
        int atIndex = -1;
        for (int i = 0; i < currentTextParts.Count; i++)
        {
            if (currentTextParts[i].Contains('@'))
            {
                atIndex = i;
                break;
            }
        }

        if (atIndex == -1)
        {
            // No @ found yet, we're in the local part
            constraints.Add(
                new MaskPartConstraint<char>(
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._+-@".ToCharArray(),
                    0,
                    64
                )
            );
            constraints.Add(new MaskPartConstraint<char>("@".ToCharArray(), 0, 1));
            constraints.Add(new MaskPartConstraint<char>("".ToCharArray(), 0, 0));
            constraints.Add(new MaskPartConstraint<char>("".ToCharArray(), 0, 0));
            constraints.Add(new MaskPartConstraint<char>("".ToCharArray(), 0, 0));
        }
        else
        {
            // @ found, determine structure
            if (atIndex == 0)
            {
                // Part 0 is @, so local part should be empty
                constraints.Add(
                    new MaskPartConstraint<char>(
                        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._+-".ToCharArray(),
                        0,
                        64
                    )
                );
            }
            else
            {
                // Part 0 has local part content
                constraints.Add(
                    new MaskPartConstraint<char>(
                        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._+-".ToCharArray(),
                        1,
                        64
                    )
                );
            }

            // Part 1: @ symbol (required)
            constraints.Add(new MaskPartConstraint<char>("@".ToCharArray(), 1, 1));

            // Check for domain dot
            int dotIndex = -1;
            for (int i = atIndex + 1; i < currentTextParts.Count; i++)
            {
                if (currentTextParts[i].Contains('.'))
                {
                    dotIndex = i;
                    break;
                }
            }

            if (dotIndex == -1)
            {
                // No dot found, domain part only
                constraints.Add(
                    new MaskPartConstraint<char>(
                        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-".ToCharArray(),
                        0,
                        255
                    )
                );
                constraints.Add(new MaskPartConstraint<char>(".".ToCharArray(), 0, 1));
                constraints.Add(new MaskPartConstraint<char>("".ToCharArray(), 0, 0));
            }
            else
            {
                // Domain and TLD parts
                constraints.Add(
                    new MaskPartConstraint<char>(
                        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-".ToCharArray(),
                        1,
                        255
                    )
                );
                constraints.Add(new MaskPartConstraint<char>(".".ToCharArray(), 1, 1));
                constraints.Add(
                    new MaskPartConstraint<char>(
                        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(),
                        2,
                        63
                    )
                );
            }
        }

        return constraints;
    }
}

/// <summary>
/// Date input constrainer for testing Mask functionality
/// </summary>
public class DateInputConstrainer : IConstrainer<char>
{
    public IReadOnlyList<MaskPartConstraint<char>> GetConstraints(
        IReadOnlyList<IReadOnlyList<char>> currentTextParts
    )
    {
        // Date format: dd.MM.yyyy
        // Part 0: First digit of day (0-3)
        // Part 1: Second digit of day (0-9, depends on first digit)
        // Part 2: First digit of month (0-1)
        // Part 3: Second digit of month (0-9, depends on first digit)
        // Part 4: Year (4 digits, 0-9)

        var constraints = new List<MaskPartConstraint<char>>();

        // Day first digit: 0-3
        constraints.Add(new MaskPartConstraint<char>("0123".ToCharArray(), 1, 1));

        // Day second digit: 0-9 (but depends on first digit)
        var firstDayDigit =
            currentTextParts.Count > 0 && currentTextParts[0].Count > 0
                ? currentTextParts[0][0]
                : '0';

        // Logic for day constraints:
        // If first digit is 0: second digit can be 1-9 (days 01-09)
        // If first digit is 1: second digit can be 0-9 (days 10-19)
        // If first digit is 2: second digit can be 0-9 (days 20-29)
        // If first digit is 3: second digit can be 0-1 (days 30-31)
        // Any other value: allow 0-9 (fallback)
        string daySecondDigitConstraints = firstDayDigit switch
        {
            '0' => "123456789", // Days 01-09
            '1' => "0123456789", // Days 10-19
            '2' => "0123456789", // Days 20-29
            '3' => "01", // Days 30-31
            _ => "0123456789", // Fallback
        };

        constraints.Add(
            new MaskPartConstraint<char>(daySecondDigitConstraints.ToCharArray(), 1, 1)
        );

        // Month first digit: 0-1
        constraints.Add(new MaskPartConstraint<char>("01".ToCharArray(), 1, 1));

        // Month second digit: depends on first digit
        var firstMonthDigit =
            currentTextParts.Count > 2 && currentTextParts[2].Count > 0
                ? currentTextParts[2][0]
                : '0';

        if (firstMonthDigit == '0')
        {
            constraints.Add(new MaskPartConstraint<char>("123456789".ToCharArray(), 1, 1));
        }
        else if (firstMonthDigit == '1')
        {
            constraints.Add(new MaskPartConstraint<char>("012".ToCharArray(), 1, 1));
        }
        else
        {
            constraints.Add(new MaskPartConstraint<char>("0123456789".ToCharArray(), 1, 1));
        }

        // Year: 4 digits
        constraints.Add(new MaskPartConstraint<char>("0123456789".ToCharArray(), 4, 4));

        return constraints;
    }
}
#endregion
