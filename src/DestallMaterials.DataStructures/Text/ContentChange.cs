namespace DestallMaterials.WheelProtection.DataStructures.Text;

public record struct ContentChange<T>(int At, int Removed, IReadOnlyList<T> Inserted)
{
    public static ContentChange<T> Get(IReadOnlyList<T> start, IReadOnlyList<T> finish) =>
        Get(start, finish, EqualityComparer<T>.Default);

    public static ContentChange<T> Get(
        IReadOnlyList<T> start,
        IReadOnlyList<T> finish,
        IEqualityComparer<T> equalityComparer
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

        return new(at, removed, inserted);
    }
}