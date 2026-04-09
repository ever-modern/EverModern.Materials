using System.Text.RegularExpressions;

namespace EverModern.WheelProtection.Extensions.Strings;

/// <summary>
/// Provides convenience string extensions.
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// Pads a string on both sides to the total length.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <param name="totalLength">The desired total length.</param>
    /// <param name="padChar">The padding character.</param>
    public static string PadCenter(this string str, int totalLength, char padChar = '-')
    {
        int padAmount = totalLength - str.Length;

        if (padAmount <= 1)
        {
            if (padAmount == 1)
            {
                return str.PadRight(totalLength, padChar);
            }
            return str;
        }

        int padLeft = padAmount / 2 + str.Length;

        return str.PadLeft(padLeft, padChar).PadRight(totalLength, padChar);
    }

    /// <summary>
    /// Ensures the string starts with the specified prefix.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="desiredBeginning">The required prefix.</param>
    public static string MustStartWith(this string input, string desiredBeginning)
    {
        if (string.IsNullOrEmpty(input))
        {
            return desiredBeginning;
        }
        if (input.StartsWith(desiredBeginning))
        {
            return input;
        }
        return desiredBeginning + input;
    }

    /// <summary>
    /// Ensures the string starts with the specified character.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="desiredBeginning">The required prefix character.</param>
    public static string MustStartWith(this string input, char desiredBeginning)
    {
        if (string.IsNullOrEmpty(input))
        {
            return desiredBeginning.ToString();
        }
        if (input.StartsWith(desiredBeginning))
        {
            return input;
        }
        return desiredBeginning + input;
    }

    /// <summary>
    /// Removes a prefix if present.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="undesiredBeginning">The prefix to remove.</param>
    public static string MustNotStartWith(this string input, string undesiredBeginning)
    {
        if (input.Length == 0)
        {
            return input;
        }
        if (!input.StartsWith(undesiredBeginning))
        {
            return input;
        }
        return new string([.. input.Skip(undesiredBeginning.Length)]).MustNotStartWith(
            undesiredBeginning
        );
    }

    /// <summary>
    /// Removes a suffix if present.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="undesiredEnding">The suffix to remove.</param>
    public static string MustNotEndWith(this string input, string undesiredEnding)
    {
        if (input.Length == 0)
        {
            return input;
        }
        if (!input.EndsWith(undesiredEnding))
        {
            return input;
        }
        return new string([.. input.Take(input.Length - undesiredEnding.Length)]);
    }

    /// <summary>
    /// Removes a trailing character if present.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="undesiredEnding">The trailing character.</param>
    public static string MustNotEndWith(this string input, char undesiredEnding)
    {
        if (input.Length == 0)
        {
            return input;
        }
        if (!input.EndsWith(undesiredEnding))
        {
            return input;
        }
        return new string([.. input.Take(input.Length - 1)]);
    }

    /// <summary>
    /// Removes any trailing characters from a set.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="undesiredEndings">The characters to remove.</param>
    public static string MustNotEndWith(this string input, IEnumerable<char> undesiredEndings)
    {
        if (input.Length == 0)
        {
            return input;
        }
        IList<char> inputString = [.. input];
        foreach (var undesiredEnding in undesiredEndings)
        {
            if (inputString[inputString.Count - 1] != undesiredEnding)
            {
                continue;
            }
            inputString.RemoveAt(inputString.Count - 1);
        }
        return new string([.. inputString]);
    }

    /// <summary>
    /// Removes any leading characters from a set.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="undesiredBeginnings">The characters to remove.</param>
    public static string MustNotStartWith(this string input, IEnumerable<char> undesiredBeginnings)
    {
        if (input.Length == 0)
        {
            return input;
        }
        IList<char> inputString = [.. input];
        foreach (var undesiredBeginning in undesiredBeginnings)
        {
            if (inputString.Count == 0)
            {
                break;
            }
            if (inputString[0] != undesiredBeginning)
            {
                continue;
            }
            inputString.RemoveAt(0);
        }
        return new string([.. inputString]);
    }

    /// <summary>
    /// Ensures the string ends with the specified character.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="desiredEnding">The required ending character.</param>
    public static string MustEndWith(this string input, char desiredEnding)
    {
        if (input.EndsWith(desiredEnding))
        {
            return input;
        }
        return input + desiredEnding;
    }

    /// <summary>
    /// Ensures the string ends with the specified suffix.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="desiredEnding">The required suffix.</param>
    public static string MustEndWith(this string input, string desiredEnding)
    {
        if (input.EndsWith(desiredEnding))
        {
            return input;
        }
        return input + desiredEnding;
    }

    /// <summary>
    /// Surrounds the string with a character if needed.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="surroundingSymbol">The surrounding character.</param>
    public static string SurroundBy(this string input, char surroundingSymbol)
    {
        if (input == null)
        {
            return "";
        }

        if (input == "")
        {
            return surroundingSymbol + input + surroundingSymbol;
        }

        if (!input.EndsWith(surroundingSymbol))
        {
            input = input + surroundingSymbol;
        }
        if (!input.StartsWith(surroundingSymbol))
        {
            input = surroundingSymbol + input;
        }
        return input;
    }

    /// <summary>
    /// Determines whether a substring appears as a separate token.
    /// </summary>
    /// <param name="wholeString">The full string.</param>
    /// <param name="match">The substring to match.</param>
    public static bool ContainsAsSeparate(this string wholeString, string match)
    {
        wholeString = wholeString.ToLower();
        match = match.ToLower();
        bool result =
            wholeString.Contains(match.MustEndWith(' '))
            || wholeString.Contains(match.MustStartWith(' '));
        return result;
    }

    /// <summary>
    /// Determines whether a string has content.
    /// </summary>
    /// <param name="input">The input string.</param>
    public static bool HasContent(this string? input) => !string.IsNullOrEmpty(input);

    /// <summary>
    /// Determines whether a string is null or empty.
    /// </summary>
    /// <param name="input">The input string.</param>
    public static bool IsEmpty(this string? input) => string.IsNullOrEmpty(input);

    /// <summary>
    /// Joins strings with a separator.
    /// </summary>
    /// <param name="input">The strings to join.</param>
    /// <param name="joiner">The separator.</param>
    public static string Merge(this IEnumerable<string> input, string joiner) =>
       string.Join(joiner, input);

    /// <summary>
    /// Joins strings with a separator.
    /// </summary>
    /// <param name="input">The strings to join.</param>
    /// <param name="joiner">The separator.</param>
    public static string Merge(this IEnumerable<string> input, char joiner) =>
        string.Join(joiner, input);


    static readonly Regex _digitsRegex = GetDigitsRegex();

    /// <summary>
    /// Determines whether the string consists only of digits.
    /// </summary>
    /// <param name="input">The input string.</param>
    public static bool ConsistsOnlyOfDigits(this string input) => _digitsRegex.IsMatch(input);

    /// <summary>
    /// Replaces matches using a regular expression.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <param name="regex">The regex.</param>
    /// <param name="replacePattern">The replacement pattern.</param>
    public static string Replace(this string str, Regex regex, string replacePattern)
    {
        var result = regex.Replace(str, replacePattern);
        return result;
    }

    /// <summary>
    /// Determines whether the string ends with any of the specified endings.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <param name="endings">The endings.</param>
    public static bool EndsWith(this string str, params string[] endings)
        => endings.Any(ending => str.EndsWith(ending));

    [GeneratedRegex("^[0-9]*$", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex GetDigitsRegex();
}
