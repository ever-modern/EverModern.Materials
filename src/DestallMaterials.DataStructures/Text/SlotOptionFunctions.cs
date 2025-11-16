using System.Runtime.InteropServices;

namespace DestallMaterials.WheelProtection.DataStructures.Text;

public static class SlotOptionFunctions
{
    public static char[] GetOptionsForSlot(
        int slotIndex,
        ReadOnlySpan<char> currentFilling,
        byte length,
        int min,
        int max
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(slotIndex, length - 1);

        long already = 0;
        for (int i = 0; i < length; i++)
        {
            var c = currentFilling[i];
            if (i == slotIndex || char.IsDigit(c) == false)
            {
                continue;
            }

            already += (long)Math.Pow(10, length - i - 1) * ToNumber(c);
        }

        var posDiv = (long)Math.Pow(10, length - slotIndex - 1);
        posDiv = posDiv < 1 ? 1 : posDiv;

        // For the current slot, we need to consider what digit we're trying to place
        // Calculate the range of valid digits for this position
        var (from, to) = ((min - already) / posDiv, (max - already) / posDiv);

        // Clamp to valid digit range
        from = from < 0 ? 0 : from > 9 ? 9 : from;
        to = to < 0 ? 0 : to > 9 ? 9 : to;

        // If no valid options in range, we need to handle this specially
        // This might mean we're dealing with an overflow situation
        if (from > to)
        {
            // When no valid digits exist for this position in isolation,
            // we need to find the most appropriate digit that could work
            // with adjustments to remaining positions
            
            // Try to find a digit that could work with minimal remaining adjustments
            var bestOptions = new List<char>();
            
            // Try each digit from 0-9 and see if we can make it work
            for (int digit = 0; digit <= 9; digit++)
            {
                // Temporarily place this digit and see if we can adjust remaining positions
                long tempValue = 0;
                for (int i = 0; i < length; i++)
                {
                    long digitValue;
                    if (i == slotIndex)
                    {
                        digitValue = digit * (long)Math.Pow(10, length - i - 1);
                    }
                    else if (char.IsDigit(currentFilling[i]))
                    {
                        digitValue = ToNumber(currentFilling[i]) * (long)Math.Pow(10, length - i - 1);
                    }
                    else
                    {
                        digitValue = 0; // assume we can fill with 0s
                    }
                    tempValue += digitValue;
                }
                
                // Check if this digit could work (even with optimal remaining digits)
                if (tempValue >= min && tempValue <= max)
                {
                    bestOptions.Add(ToChar(digit));
                }
            }
            
            if (bestOptions.Count > 0)
            {
                return [.. bestOptions];
            }
            
            // If still no options, fall back to 0-9 for the mask to handle
            return [];
        }

        char[] result = [.. Enumerable.Range((int)from, (int)(to - from + 1)).Select(ToChar)];

        return result;
    }

    public static char[][] ToCharOptionsOld(
        int minValue,
        int maxValue,
        byte length,
        ReadOnlySpan<char> currentFilling
    )
    {
        char[][] result = new char[length][];

        var prevDiff = PreviousFillingSituation.TopAndBottomAreSame;

        for (int i = length - 1; i >= 0; i--)
        {
            var divider = (int)Math.Pow(10, i);
            var minDigit = (minValue / divider) % 10;
            var maxDigit = (maxValue / divider) % 10;

            var (from, to) = prevDiff switch
            {
                PreviousFillingSituation.AtTop => (0, maxDigit),
                PreviousFillingSituation.AtBottom => (minDigit, 9),
                PreviousFillingSituation.NarrowOptions => (maxDigit, minDigit),
                PreviousFillingSituation.TopAndBottomAreSame => (minDigit, maxDigit),
                PreviousFillingSituation.Middle => (0, 9),
                _ => (0, 9),
            };

            var at = length - 1 - i;

            if (from <= to)
            {
                result[at] = [.. Enumerable.Range(from, (to - from + 1)).Select(ToChar)];
            }
            else
            {
                result[at] =
                [
                    .. Enumerable.Range(from, 10 - from).Select(ToChar),
                    .. Enumerable.Range(0, to + 1).Select(ToChar),
                ];
            }

            var currentChar = currentFilling[at];

            if (from == to)
            {
                prevDiff = PreviousFillingSituation.TopAndBottomAreSame;
            }
            else if (char.IsDigit(currentChar))
            {
                var current = ToNumber(currentChar);

                prevDiff =
                    current == from ? PreviousFillingSituation.AtBottom
                    : current == to ? PreviousFillingSituation.AtTop
                    : PreviousFillingSituation.Middle;
            }
            else
            {
                prevDiff =
                    to
                    - from switch
                    {
                        0 => PreviousFillingSituation.TopAndBottomAreSame,
                        1 => PreviousFillingSituation.NarrowOptions,
                        _ => PreviousFillingSituation.Middle,
                    };
            }
        }

        if (int.TryParse(currentFilling, out var currentValue))
        {
            if (currentValue > maxValue)
            {
                var difference = currentValue - maxValue;

                var differenceLength = difference.ToString().Length;

                var adaptFrom = length - differenceLength;

                char[] values = [.. currentFilling];

                for (int i = adaptFrom - 1; i >= 0; i--)
                {
                    var options = result[i];

                    var overflowSolvingOptions = options
                        .Where(char.IsDigit)
                        .SkipWhile(c =>
                        {
                            char[] withOption = [.. values[..i], c, .. values[(i + 1)..]];
                            var valueWithOption = int.Parse(withOption);
                            return valueWithOption > maxValue;
                        })
                        .ToArray();

                    if (overflowSolvingOptions.Length == 0)
                    {
                        result[i] = [options[0]];
                    }
                    else
                    {
                        result[i] = overflowSolvingOptions;
                        break;
                    }
                }
            }
            else if (currentValue < minValue) { }
        }

        return result;
    }

    public static byte ToNumber(this char digit) => (byte)(digit - '0');

    public static char ToChar(this int digit) => (char)((digit % 10) + '0');

    enum PreviousFillingSituation
    {
        AtBottom = 1,
        NarrowOptions = 2,
        TopAndBottomAreSame = 3,
        AtTop = 4,
        Middle = 5,
    }
}
