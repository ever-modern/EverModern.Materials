namespace DestallMaterials.WheelProtection.DataStructures.Text;

public record struct MaskValuePiece(char? Symbol, string? Group)
{
    public bool Filled => Symbol is not null || Group is not null;
}

public class Mask(ITextConstrainer constrainer)
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
    /// <param name="oldValue">Previous text value</param>
    /// <param name="newValue">New text value</param>
    /// <returns></returns>
    public ProjectionPiece[] MakeProjection(char?[] oldValue, char?[] newValue)
    {
        /* This method reflects the logic of an input value getting changed.
         * The most general case - some part of current value get selected and then user
         * pastes something from the clipboard - pasted symbols array may be shorter or longer the selected symbols.
         * Technique: calculate the difference first.
         * Then apply the difference onto the old value.
         * First, remove the symbols, that were allowed to be removed -
         * one by one asking constrainer for allowed symbols after each deletion.
         * If a character removed in newValue could not be removed due to constraints,
         * skip this one and move to the next.
         * If a symbol could be deleted, make it an empty spot and go to the next.
         * When deletion part is complete, do the insertion, following the same principles:
         * insert one by one, asking for constraints after each change
         * skip invalid values
           if constraints assume single-option values of single-option length - paste them each turn.
            Write step reports into Diagnostics.
        */

        throw new NotImplementedException();
    }

    public static (int DiffStart, int EqualStart) FindStringDifference(string first, string second)
    {
        if (first == null || second == null)
            throw new ArgumentNullException("Input strings cannot be null");

        int minLength = Math.Min(first.Length, second.Length);
        int diffStart = -1;
        int equalStart = -1;

        // Find where they start differing
        for (int i = 0; i < minLength; i++)
        {
            if (first[i] != second[i])
            {
                diffStart = i;
                break;
            }
        }

        // If no difference found in common length
        if (diffStart == -1)
        {
            // Strings are equal up to minLength
            if (first.Length == second.Length)
            {
                // Strings are completely equal
                return (first.Length, first.Length);
            }
            else
            {
                // One string is prefix of the other
                return (minLength, minLength);
            }
        }

        // Find where they become equal again after the difference
        for (int i = diffStart; i < minLength; i++)
        {
            if (first[i] == second[i])
            {
                equalStart = i;
                break;
            }
        }

        // If they don't become equal again within the common length
        if (equalStart == -1)
        {
            equalStart = minLength;
        }

        return (diffStart, equalStart);
    }
}

/// <summary>
/// Bears either a constant (already filled) or variable (not yet filled) value.
/// </summary>
public struct ProjectionPiece
{
    public string? ConstantValue { get; }
    public TextConstraint? VariableValue { get; }

    public ProjectionPiece(string constantValue) => ConstantValue = constantValue;

    public ProjectionPiece(TextConstraint variableValue) => VariableValue = variableValue;

    public static implicit operator ProjectionPiece(string other) => new(other);

    public static implicit operator ProjectionPiece(TextConstraint variable) => new(variable);
}

/// <summary>
///
/// </summary>
/// <param name="AllowedSymbols"></param>
/// <param name="MinLenght"></param>
/// <param name="MaxLength"></param>
public record struct TextConstraint(
    IReadOnlyList<char> AllowedSymbols,
    int MinLenght,
    int MaxLength
);

/// <summary>
/// Returns constraints on text values, based on already present text content.
/// </summary>
public interface ITextConstrainer
{
    public IReadOnlyList<TextConstraint> GetConstraints(ReadOnlySpan<char?> currentSymbols);
}

public class PhoneNumberConstrainer : ITextConstrainer
{
    public IReadOnlyList<TextConstraint> GetConstraints(ReadOnlySpan<char?> currentSymbols)
    {
        /*Implement a simple phone number constraints for test purposes. */
        throw new NotImplementedException();
    }
}
