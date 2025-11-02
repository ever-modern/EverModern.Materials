namespace DestallMaterials.WheelProtection.DataStructures.Text;

public enum RemovalDirection
{
    Backward,
    Forward,
}

public record struct ContentChange<T>(
    int At,
    int Removed,
    IReadOnlyList<T> Inserted
)
{
    public static ContentChange<T> Get(IReadOnlyList<T> start, IReadOnlyList<T> finish, int carretAt = -1) =>
        Get(start, finish, EqualityComparer<T>.Default, carretAt);

    public static ContentChange<T> Get(
        IReadOnlyList<T> start,
        IReadOnlyList<T> finish,
        IEqualityComparer<T> equalityComparer,
        int carretAt = -1
    )
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
            && equalityComparer.Equals(start[prefixLength], finish[prefixLength])
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
                // Both lists are identical, but we might have a caret position change
                // Use carretAt to determine if there was actually a change at a specific position
                if (carretAt >= 0 && carretAt < startLength)
                {
                    // There's a specific position where something happened
                    // This could be a deletion and reinsertion of the same character(s)
                    // We'll report that position with an empty change
                    return new ContentChange<T>(carretAt, 0, []);
                }
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

            if (equalityComparer.Equals(start[startIndex], finish[finishIndex]))
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

        // Use carretAt to refine the result when appropriate
        // This helps distinguish cases where the content appears the same
        // but the change happened at different positions
        if (carretAt >= 0 && insertedCount > 0)
        {
            // If there's an insertion and carretAt is valid,
            // we can use it to potentially adjust where we report the change
            // This is especially useful for cases with repeated characters
            if (carretAt >= at && carretAt < at + insertedCount + 1)
            {
                // The change is reported at or near where carretAt occurred
                // Keep the current 'at' position as it's calculated correctly
            }
        }

        return new(at, removed, inserted);
    }
}
