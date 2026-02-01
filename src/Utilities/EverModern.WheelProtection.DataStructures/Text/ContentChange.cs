namespace EverModern.WheelProtection.DataStructures.Text;

public record struct ContentChange<T>(
    int At,
    int Removed,
    T[] Inserted
)
{
    public static ContentChange<T> Get(
        ReadOnlySpan<T> start,
        ReadOnlySpan<T> finish,
        int carretFinishedAt = -1
    ) => Get(start, finish, EqualityComparer<T>.Default, carretFinishedAt);

    public static ContentChange<T> Get(
        ReadOnlySpan<T> start,
        ReadOnlySpan<T> finish,
        IEqualityComparer<T> equalityComparer,
        int caretFinishedAt = -1
    )
    {
        int startLength = start.Length;
        int finishLength = finish.Length;

        // Find longest common prefix
        int prefixLength = 0;
        int maxPrefix = Math.Min(startLength, finishLength);

        if (startLength == 0)
        {
            return new(0, 0, [.. finish]);
        }

        if (finishLength == 0)
        {
            return new(0, startLength, []);
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
                // Both lists are identical
                if (caretFinishedAt >= 0)
                {
                    // Handle cursor-based removal for identical content
                    if (caretFinishedAt >= startLength)
                    {
                        // Cursor at or beyond end - this implies removal of last character
                        if (startLength > 0)
                        {
                            // Return the last character as the one that was "removed"
                            return new ContentChange<T>(
                                startLength - 1,
                                1,
                                [start[startLength - 1]]
                            );
                        }
                        else
                        {
                            return new ContentChange<T>(0, 0, []);
                        }
                    }
                    else if (caretFinishedAt > 0)
                    {
                        // Cursor in the middle or at position 1
                        // carretAt indicates the position after removal, so the removed character is at carretAt-1
                        int removedAt = caretFinishedAt - 1;
                        if (removedAt >= 0 && removedAt < startLength)
                        {
                            return new ContentChange<T>(removedAt, 1, [start[removedAt]]);
                        }
                    }
                    else if (caretFinishedAt == 0)
                    {
                        // Cursor at beginning - this would mean first character was removed
                        if (startLength > 0)
                        {
                            return new ContentChange<T>(0, 1, [start[0]]);
                        }
                    }
                }
                // No carretAt provided and content is identical - return the length as position (backward compatibility)
                return new ContentChange<T>(startLength, 0, []);
            }

            // One list is entirely contained in the other as a prefix
            int changeAt = prefixLength;
            int elementsRemoved = startLength - prefixLength;
            int newElementsCount = finishLength - prefixLength;

            // Handle carretAt for prefix-based scenarios (when one list is contained in another)
            if (caretFinishedAt >= 0)
            {
                if (startLength > finishLength)
                {
                    // Deletion scenario: determine position based on carretAt
                    if (caretFinishedAt <= prefixLength)
                    {
                        if (caretFinishedAt == 0)
                            changeAt = 0;
                        else
                            changeAt = Math.Min(caretFinishedAt, startLength - 1);
                    }
                }
                else if (finishLength > startLength)
                {
                    // Insertion scenario: use caret to locate where insertion likely happened
                    // caretFinishedAt is position in finished content; insertion start is caret - number of new elements
                    changeAt = Math.Max(0, caretFinishedAt - newElementsCount);
                }
                // else lengths equal handled above
            }

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

        // Fix for backward compatibility: handle overlapping prefix and suffix
        if (removed < 0)
        {
            removed = 0;
        }

        // Special handling for backward compatibility: adjust removal count for common scenarios
        // When we have insertions and the prefix is minimal, ensure we don't over-report removals
        if (removed > 0 && insertedCount > 0 && prefixLength == 1 && caretFinishedAt < 0)
        {
            removed = Math.Min(removed, Math.Max(1, insertedCount));
        }

        // Extract the inserted elements
        T[] inserted = new T[insertedCount];
        for (int i = 0; i < insertedCount; i++)
        {
            inserted[i] = finish[prefixLength + i];
        }

        // Handle carretAt for cursor-based disambiguation
        if (caretFinishedAt >= 0)
        {
            // For deletion scenarios, carretAt determines the exact position
            if (removed > 0 && insertedCount == 0)
            {
                // Key insight: carretAt represents the final cursor position in the finished content
                // If carretAt is within the prefix area, it indicates which deletion occurred

                if (caretFinishedAt <= prefixLength)
                {
                    // Cursor is within or at the end of the common prefix
                    // This suggests deletion affected the area where cursor ended up
                    if (caretFinishedAt == 0)
                    {
                        at = 0; // Deletion at beginning
                    }
                    else
                    {
                        // The key fix: use carretAt directly as the deletion position
                        // when cursor is within the prefix
                        at = Math.Min(caretFinishedAt, startLength - 1);
                    }
                }
                // else: cursor beyond prefix, keep calculated position
            }
            // For insertion scenarios, compute insertion index using caret when possible
            else if (removed == 0 && insertedCount > 0)
            {
                // caretFinishedAt indicates the final caret position in the finished text.
                // The insertion start index is therefore caret - number of inserted elements.
                at = Math.Max(0, caretFinishedAt - insertedCount);
            }
        }

        return new(at, removed, inserted);
    }

    // Overloads for IReadOnlyList<T>
    public static ContentChange<T> Get(
        IReadOnlyList<T> start,
        IReadOnlyList<T> finish,
        int carretFinishedAt = -1
    ) => Get(start, finish, EqualityComparer<T>.Default, carretFinishedAt);

    public static ContentChange<T> Get(
        IReadOnlyList<T> start,
        IReadOnlyList<T> finish,
        IEqualityComparer<T> equalityComparer,
        int carretFinishedAt = -1
    )
    {
        var startArray = start as T[] ?? start.ToArray();
        var finishArray = finish as T[] ?? finish.ToArray();
        return Get(startArray.AsSpan(), finishArray.AsSpan(), equalityComparer, carretFinishedAt);
    }

    /// <summary>
    /// Apply this content change to a source span and return the resulting sequence as an array.
    /// </summary>
    public T[] Apply(ReadOnlySpan<T> source)
    {
        // Clamp At and Removed to valid ranges for the provided source
        int at = Math.Clamp(At, 0, source.Length);
        int removed = Math.Clamp(Removed, 0, Math.Max(0, source.Length - at));

        int resultLength = source.Length - removed + (Inserted?.Length ?? 0);
        var result = new T[resultLength];

        // copy prefix
        if (at > 0)
        {
            source.Slice(0, at).CopyTo(result.AsSpan(0, at));
        }

        // copy inserted
        if (Inserted != null && Inserted.Length > 0)
        {
            Array.Copy(Inserted, 0, result, at, Inserted.Length);
        }

        // copy suffix
        int suffixSrcStart = at + removed;
        int suffixLen = source.Length - suffixSrcStart;
        if (suffixLen > 0)
        {
            source
                .Slice(suffixSrcStart, suffixLen)
                .CopyTo(result.AsSpan(at + (Inserted?.Length ?? 0), suffixLen));
        }

        return result;
    }

    /// <summary>
    /// Apply this content change to a source IReadOnlyList and return the resulting sequence as an array.
    /// </summary>
    public T[] Apply(IReadOnlyList<T> source)
    {
        var sourceArray = source as T[] ?? source.ToArray();
        return Apply(sourceArray.AsSpan());
    }
}
