global using MaskPart = System.Collections.Generic.List<char>;
global using MaskParts = System.Collections.Generic.IReadOnlyList<System.Collections.Generic.List<char>>;

namespace DestallMaterials.WheelProtection.DataStructures.Text;

public class Mask(ITextConstrainer constrainer, MaskParts parts)
{
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
    /// Makes projection of mask onto a new string value, based on the difference with the previos string value.
    /// </summary>
    /// <param name="initValue">Previous text value</param>
    /// <param name="targerValue">New text value</param>
    /// <returns></returns>
    public (int PartIndex, int ItemIndex) ChangePart(int partIndex, IReadOnlyList<char> targerValue)
    {
        var (at, deleted, inserted) = GetChange(parts[partIndex], targerValue);

        for (int i = 0; i < deleted; i++)
        {
            var constraints = constrainer.GetConstraints(initValue);
            var (allowedValues, minLength, maxLength) = constraints[at + i];
            DecideNoOptions(allowedValues, constraints);
        }
    }

    static ContentChange<char> GetChange(IReadOnlyList<char> oldValue, IReadOnlyList<char> newValue) =>
        ContentChange<char>.Get(oldValue, newValue);

    void DeleteOne(List<char?> symbols, int at)
    {
        var constraints = constrainer.GetConstraints(symbols);

    }

    /// <summary>
    /// Paste values into spots for which there can be only one value.
    /// </summary>
    /// <param name="symbols"></param>
    /// <param name="constraints"></param>
    public static void DecideNoOptions(List<char?> symbols, IReadOnlyList<TextPartConstraint> constraints)
    {
        var l = constraints.Count;
        var cIndex = 0;
        for (int i = 0; i < l && cIndex < symbols.Count; i++)
        {
            var (allowedSymbols, minLength, maxLength) = constraints[i];
            var symbolsForConstraint = symbols[cIndex];
            char? symbolToPaste = allowedSymbols.Count == 1 ? allowedSymbols[0] : null;
            var addSymbols = cIndex + minLength - symbols.Count;
            if (addSymbols > 0)
            {
                symbols.InsertRange(cIndex, Enumerable.Repeat(symbolToPaste, addSymbols));
                cIndex += addSymbols;
            }
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
/// <param name="AllowedSymbols"></param>
/// <param name="MinLength"></param>
/// <param name="MaxLength"></param>
public record struct TextPartConstraint(
    IReadOnlyList<char> AllowedSymbols,
    int MinLength,
    int MaxLength
);

/// <summary>
/// Returns constraints on text values, based on already present text content.
/// </summary>
public interface ITextConstrainer
{
    public IReadOnlyList<TextPartConstraint> GetConstraints(IReadOnlyList<IReadOnlyList<char>> currentTextParts);
}

public class PhoneNumberConstrainer : ITextConstrainer
{
    public IReadOnlyList<TextPartConstraint> GetConstraints(IReadOnlyList<IReadOnlyList<char>> currentSymbols)
    {
        /*Implement a simple phone number constraints for test purposes. */
        throw new NotImplementedException();
    }
}
