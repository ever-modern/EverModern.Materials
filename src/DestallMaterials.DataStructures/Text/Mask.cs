namespace DestallMaterials.WheelProtection.DataStructures.Text;

public class Mask<T>(
    IConstrainer<T> constrainer,
    IReadOnlyList<List<T>> parts,
    IEqualityComparer<T> equalityComparer
)
{
    public Mask(IConstrainer<T> textConstrainer, IReadOnlyList<List<T>> parts)
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
                Log($"Part {currentPosition} reached max length {constraint.MaxLength}, trying next");
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
    public (int PartIndex, int ItemIndex) UpdateConstraints(int partIndex, MaskPartConstraint<T> newConstraints)
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
            var allowedValuesList = partIndex < constraints.Count 
                ? constraints[partIndex].AllowedValues 
                : null;
            
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
                if (firstAllowed != null)
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
            part.Add(newConstraints.AllowedValues.First());
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
    public (int PartIndex, int ItemIndex) ProcessContentChange(int partIndex, ContentChange<T> change)
    {
        Log($"Processing ContentChange: part={partIndex}, at={change.At}, removed={change.Removed}, inserted={change.Inserted.Count}");

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
    public static void DecideSingleOptions(
        List<T> symbols,
        MaskPartConstraint<T> constraints
    )
    {
        // If the part is empty and has min length > 0, add the first allowed value
        if (symbols.Count < constraints.MinLength && constraints.AllowedValues.Count > 0)
        {
            symbols.Add(constraints.AllowedValues.First());
        }
    }
}

public record struct ContentChange<T>(int At, int Removed, IReadOnlyList<T> Inserted)
{
    public static ContentChange<T> Get(IReadOnlyList<T> start, IReadOnlyList<T> finish)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(finish);

        int startLength = start.Count;
        int finishLength = finish.Count;

        // Find longest common prefix
        int prefixLength = 0;
        int maxPrefix = Math.Min(startLength, finishLength);

        if (start.Count == 0)
        {
            return new(0, 0, finish);
        }

        if (finish.Count == 0)
        {
            return new(0, start.Count, []);
        }

        while (
            prefixLength < maxPrefix
            && EqualityComparer<T>.Default.Equals(start[prefixLength], finish[prefixLength])
        )
        {
            prefixLength++;
        }

        // Special case: if the entire shorter list matches the prefix
        if (prefixLength == startLength || prefixLength == finishLength)
        {
            // If both lists are identical (prefix = entire length of both)
            if (prefixLength == startLength && prefixLength == finishLength)
            {
                return new ContentChange<T>(0, 0, []);
            }

            // One list is entirely contained in the other as a prefix
            int changeAt = prefixLength;
            int elementsRemoved = startLength - prefixLength;
            int newElementsCount = finishLength - prefixLength;

            T[] newElements = new T[newElementsCount];
            for (int i = 0; i < newElementsCount; i++)
            {
                newElements[i] = finish[prefixLength + i];
            }

            return new(changeAt, elementsRemoved, newElements);
        }

        // Find longest common suffix by working backwards
        int suffixLength = 0;
        int maxPossibleSuffix = Math.Min(startLength - prefixLength, finishLength - prefixLength);

        for (int i = 1; i <= maxPossibleSuffix; i++)
        {
            int startIndex = startLength - i;
            int finishIndex = finishLength - i;

            if (EqualityComparer<T>.Default.Equals(start[startIndex], finish[finishIndex]))
            {
                suffixLength = i;
            }
            else
            {
                break;
            }
        }

        int at = prefixLength;
        int removed = startLength - prefixLength - suffixLength;
        int insertedCount = finishLength - prefixLength - suffixLength;

        // Extract the inserted elements
        T[] inserted = new T[insertedCount];
        for (int i = 0; i < insertedCount; i++)
        {
            inserted[i] = finish[prefixLength + i];
        }

        return new(at, removed, inserted);
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
        /*Implement a simple phone number constraints for test purposes. */
        throw new NotImplementedException();
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
        constraints.Add(new MaskPartConstraint<char>(
            "0123".ToCharArray(),
            1, 1
        ));

        // Day second digit: 0-9 (but depends on first digit)
        var firstDayDigit = currentTextParts.Count > 0 && currentTextParts[0].Count > 0 
            ? currentTextParts[0][0] 
            : '0';
        
        if (firstDayDigit == '0' || firstDayDigit == '1')
        {
            constraints.Add(new MaskPartConstraint<char>(
                "0123456789".ToCharArray(),
                1, 1
            ));
        }
        else if (firstDayDigit == '2')
        {
            constraints.Add(new MaskPartConstraint<char>(
                "0123456789".ToCharArray(),
                1, 1
            ));
        }
        else if (firstDayDigit == '3')
        {
            constraints.Add(new MaskPartConstraint<char>(
                "01".ToCharArray(),
                1, 1
            ));
        }
        else
        {
            constraints.Add(new MaskPartConstraint<char>(
                "0123456789".ToCharArray(),
                1, 1
            ));
        }

        // Month first digit: 0-1
        constraints.Add(new MaskPartConstraint<char>(
            "01".ToCharArray(),
            1, 1
        ));

        // Month second digit: depends on first digit
        var firstMonthDigit = currentTextParts.Count > 2 && currentTextParts[2].Count > 0
            ? currentTextParts[2][0]
            : '0';

        if (firstMonthDigit == '0')
        {
            constraints.Add(new MaskPartConstraint<char>(
                "123456789".ToCharArray(),
                1, 1
            ));
        }
        else if (firstMonthDigit == '1')
        {
            constraints.Add(new MaskPartConstraint<char>(
                "012".ToCharArray(),
                1, 1
            ));
        }
        else
        {
            constraints.Add(new MaskPartConstraint<char>(
                "0123456789".ToCharArray(),
                1, 1
            ));
        }

        // Year: 4 digits
        constraints.Add(new MaskPartConstraint<char>(
            "0123456789".ToCharArray(),
            4, 4
        ));

        return constraints;
    }
}
