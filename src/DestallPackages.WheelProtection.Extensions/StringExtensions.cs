using System.Text.RegularExpressions;

namespace DestallMaterials.WheelProtection.Extensions.Strings;

public static class StringExtensions
{
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
        return new string(input.Skip(undesiredBeginning.Length).ToArray()).MustNotStartWith(
            undesiredBeginning
        );
    }

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
        return new string(input.Take(input.Length - undesiredEnding.Length).ToArray());
    }

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
        return new string(input.Take(input.Length - 1).ToArray());
    }

    public static string MustNotEndWith(this string input, IEnumerable<char> undesiredEndings)
    {
        if (input.Length == 0)
        {
            return input;
        }
        IList<char> inputString = input.ToList();
        foreach (var undesiredEnding in undesiredEndings)
        {
            if (inputString[inputString.Count - 1] != undesiredEnding)
            {
                continue;
            }
            inputString.RemoveAt(inputString.Count - 1);
        }
        return new string(inputString.ToArray());
    }

    public static string MustNotStartWith(this string input, IEnumerable<char> undesiredBeginnings)
    {
        if (input.Length == 0)
        {
            return input;
        }
        IList<char> inputString = input.ToList();
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
        return new string(inputString.ToArray());
    }

    public static string MustEndWith(this string input, char desiredEnding)
    {
        if (input.EndsWith(desiredEnding))
        {
            return input;
        }
        return input + desiredEnding;
    }

    public static string MustEndWith(this string input, string desiredEnding)
    {
        if (input.EndsWith(desiredEnding))
        {
            return input;
        }
        return input + desiredEnding;
    }

    public static string BoundBy(this string input, char boundary)
    {
        if (input == null)
        {
            return "";
        }

        if (input == "")
        {
            return boundary + input + boundary;
        }

        if (!input.EndsWith(boundary))
        {
            input = input + boundary;
        }
        if (!input.StartsWith(boundary))
        {
            input = boundary + input;
        }
        return input;
    }

    public static bool ContainsAsSeparate(this string wholeString, string match)
    {
        wholeString = wholeString.ToLower();
        match = match.ToLower();
        bool result =
            wholeString.Contains(match.MustEndWith(' '))
            || wholeString.Contains(match.MustStartWith(' '));
        return result;
    }

    public static bool HasContent(this string? input) => !string.IsNullOrEmpty(input);

    public static bool IsEmpty(this string? input) => string.IsNullOrEmpty(input);

    [Obsolete("Use Merge instead.")]
    public static string Join(this IEnumerable<string> input, string joiner) =>
        string.Join(joiner, input);

    [Obsolete("Use Merge instead.")]
    public static string Join(this IEnumerable<string> input, char joiner) =>
        string.Join(joiner, input);

    public static string Merge(this IEnumerable<string> input, string joiner) =>
       string.Join(joiner, input);

    public static string Merge(this IEnumerable<string> input, char joiner) =>
        string.Join(joiner, input);


    static readonly Regex _digitsRegex = new Regex(
        "^[0-9]*$",
        RegexOptions.Compiled | RegexOptions.Singleline
    );

    public static bool ConsistsOnlyOfDigits(this string input) => _digitsRegex.IsMatch(input);

    public static string Replace(this string str, Regex regex, string replacePattern)
    {
        var result = regex.Replace(str, replacePattern);
        return result;
    }

    public static bool EndsWith(this string str, params string[] endings)
        => endings.Any(ending => str.EndsWith(ending));
}
