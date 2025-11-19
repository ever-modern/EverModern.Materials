using System;
using System.Runtime.InteropServices;
using System.Linq;

namespace DestallMaterials.WheelProtection.DataStructures.Text;

public static class SlotOptionFunctions
{
    static IEnumerable<byte> NoConstraint = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    static IEnumerable<long> LongSeq(long min, long max, long pace)
    {
        for (long option = min; option <= max; option += pace)
        {
            yield return option;
        }
    }

    static char[] GetOptionsForDefiniteNumber(
        byte slotIndex,
        long currentFilling,
        byte length,
        long min,
        long max
    )
    {
        long already = currentFilling;

        var posDiv = (long)Math.Pow(10, length - slotIndex - 1);

        posDiv = posDiv < 1 ? 1 : posDiv;

        // compute raw fractional bounds for the digit value
        var rawFrom = (decimal)(min - already) / posDiv;
        var rawTo = (decimal)(max - already) / posDiv;

        // compute integer bounds (inclusive) for the digit (may be outside 0..9)
        var fromInt = (int)Math.Ceiling(rawFrom);
        var toInt = (int)Math.Floor(rawTo);

        // if the allowed integer span covers at least 10 consecutive integers,
        // then every digit 0..9 is allowed
        if (toInt - fromInt + 1 >= 10)
        {
            return DigitSpan(0, 9);
        }

        // Map the integer range to digits modulo 10 and return distinct values in order
        var result = Enumerable.Range(fromInt, toInt - fromInt + 1)
            .Select(k => (byte)((k % 10 + 10) % 10))
            .Distinct()
            .Select(b => ToChar(b))
            .ToArray();

        return result;
    }

    static char[] GetOptionsForSlotReliable(
        byte slotIndex,
        ReadOnlySpan<char> currentFilling,
        byte length,
        long min,
        long max
    )
    {
        char[] filling = [.. currentFilling];
        filling[slotIndex] = '0';

        if (long.TryParse(filling, out var definiteNumber))
        {
            return GetOptionsForDefiniteNumber(slotIndex, definiteNumber, length, min, max);
        }

        var divider = (int)Math.Pow(10, length - 1 - slotIndex);

        char[][] constraints = new char[length][];

        foreach (var (digit, index) in ToDigits(filling).Select((d, i) => (d, i)))
        {
            var thisDivider = (long)Math.Pow(10, length - index - 1);

            var isTargetSlot = index == slotIndex;

            var minDigit = (byte)((min / thisDivider) % 10);
            var maxDigit = (byte)((max / thisDivider) % 10);

            if (index == 0)
            {
                if (digit is null || isTargetSlot)
                {
                    constraints[index] = DigitSpan(minDigit, maxDigit);
                }
                else
                {
                    if (digit > maxDigit || digit < minDigit)
                    {
                        return [];
                    }
                    constraints[index] = DigitSpan(digit.Value, digit.Value);
                }

                continue;
            }

            var prevOptions = constraints[index - 1];

            char[] thisConstraint = prevOptions.Length switch
            {
                0 => [],
                1 => DigitSpan(minDigit, maxDigit),
                2 => maxDigit > minDigit ? DigitSpan(0, 9) : DigitSpan(minDigit, maxDigit),
                _ => DigitSpan(0, 9),
            };

            if (digit is not null && isTargetSlot == false)
            {
                var digitChar = ToChar(digit.Value);
                if (thisConstraint.Contains(digitChar) is false)
                {
                    return [];
                }
                else
                {
                    thisConstraint = [digitChar];
                }
            }

            constraints[index] = thisConstraint;
        }

        for (var i = length - 1; i >= 2; i--)
        {
            var constraint = constraints[i];
            if (constraint.Length == 0)
            {
                return [];
            }

            var prePrevConstraint = constraints[i - 2];

            if (constraint.Length > 1 || prePrevConstraint.Length > 2)
            {
                continue;
            }

            var digit = ToNumber(constraint[0]);

            var thisDivider = (long)Math.Pow(10, length - i - 1);

            var minDigit = (byte)((min / thisDivider) % 10);
            var maxDigit = (byte)((max / thisDivider) % 10);

            var prevConstraint = constraints[i - 1];

            prevConstraint =
                (digit > maxDigit && digit < minDigit) ? prevConstraint[1..^1]
                : digit < minDigit ? prevConstraint[1..]
                : digit > maxDigit ? prevConstraint[..^1]
                : prevConstraint;

            constraints[i - 1] = prevConstraint;
        }

        var result = constraints[slotIndex];

        return [.. result.Distinct()];
    }

    static bool IsGoodOption(
        long option,
        byte slotIndex,
        ReadOnlySpan<char> currentFilling,
        byte length
    )
    {
        for (int i = 0; i < length; i++)
        {
            if (i == slotIndex)
            {
                continue;
            }
            char c = currentFilling[i];
            if (char.IsDigit(c))
            {
                int digit = ToNumber(c);
                var divider = (long)Math.Pow(10, length - 1 - i);
                long optionDigit = (option / divider) % 10;
                if (digit != optionDigit)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public static char[] GetOptionsForSlot(
        byte slotIndex,
        ReadOnlySpan<char> currentFilling,
        byte length,
        long min,
        long max
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(slotIndex, length - 1);
        if (currentFilling.Length != length)
        {
            throw new ArgumentException(
                $"Length is different from {nameof(length)}",
                nameof(currentFilling)
            );
        }
        if (min > max)
        {
            throw new ArgumentException($"{nameof(min)} greater than {nameof(max)}");
        }

        return GetOptionsForSlotReliable((byte)slotIndex, currentFilling, length, min, max);

        char[]? result = null;

        var prevDiff = FillingSituation.TopAndBottomAreSame;

        for (int i = length - 1; i >= 0; i--)
        {
            var divider = (int)Math.Pow(10, i);
            var minDigit = (int)(min / divider) % 10;
            var maxDigit = (int)(max / divider) % 10;

            var (from, to) = prevDiff switch
            {
                FillingSituation.AtTop => (0, maxDigit),
                FillingSituation.AtBottom => (minDigit, 9),
                FillingSituation.NarrowOptions => (maxDigit, minDigit),
                FillingSituation.NarrowOptionsOverflowing => (minDigit, maxDigit),
                FillingSituation.TopAndBottomAreSame => (minDigit, maxDigit),
                FillingSituation.Middle => (0, 9),
                _ => (0, 9),
            };

            var at = length - 1 - i;

            bool isFinish = at == slotIndex;

            if (isFinish)
            {
                if (from <= to)
                {
                    result = [.. Enumerable.Range(from, (to - from + 1)).Select(ToChar)];
                }
                else
                {
                    result =
                    [
                        .. Enumerable.Range(from, 10 - from).Select(ToChar),
                        .. Enumerable.Range(0, to + 1).Select(ToChar),
                    ];
                }

                if (at == length - 1 || prevDiff == FillingSituation.Middle)
                {
                    return result;
                }

                var nextChar = currentFilling[at + 1];

                if (char.IsDigit(nextChar) == false)
                {
                    return result;
                }

                var nextNumber = ToNumber(nextChar);

                var nextDivider = (int)Math.Pow(10, i - 1);
                var nextMinDigit = (min / nextDivider) % 10;
                var nextMaxDigit = (max / nextDivider) % 10;

                var breaksTop = nextNumber > nextMaxDigit;
                var breaksBottom = nextNumber < nextMinDigit;

                result = prevDiff switch
                {
                    FillingSituation.AtTop => breaksTop ? result[0..^1] : result,
                    FillingSituation.AtBottom => breaksBottom ? result[1..] : result,
                    FillingSituation.NarrowOptions
                    or FillingSituation.NarrowOptionsOverflowing
                    or FillingSituation.TopAndBottomAreSame => breaksTop && breaksBottom
                        ? result[1..^1]
                    : breaksBottom ? result[1..]
                    : breaksTop ? result[..^1]
                    : result,
                    _ => result,
                };

                return result;
            }

            var currentChar = currentFilling[at];

            if (from == to)
            {
                if (minDigit == maxDigit)
                {
                    prevDiff = FillingSituation.TopAndBottomAreSame;
                }
                else if (minDigit == from)
                {
                    prevDiff = FillingSituation.AtBottom;
                }
                else if (maxDigit == from)
                {
                    prevDiff = FillingSituation.AtTop;
                }
                else
                {
                    throw new InvalidOperationException("Unexpected turn");
                }
            }
            else if (char.IsDigit(currentChar))
            {
                var current = ToNumber(currentChar);

                prevDiff =
                    current == from ? FillingSituation.AtBottom
                    : current == to ? FillingSituation.AtTop
                    : FillingSituation.Middle;
            }
            else
            {
                var diff = (
                    prevDiff == FillingSituation.NarrowOptions ? (from - to + 10) : (to - from)
                );
                prevDiff = diff switch
                {
                    0 => FillingSituation.TopAndBottomAreSame,
                    1 => minDigit > maxDigit
                        ? FillingSituation.NarrowOptionsOverflowing
                        : FillingSituation.NarrowOptions,
                    _ => FillingSituation.Middle,
                };
            }
        }

        return result ?? throw new InvalidOperationException();
    }

    static char[] GetOptionsForSlotFromLeft(
        int slotIndex,
        ReadOnlySpan<char> currentFilling,
        byte length,
        int min,
        int max
    )
    {
        char[]? result = null;

        var prevDiff = FillingSituation.TopAndBottomAreSame;

        for (int i = length - 1; i >= 0; i--)
        {
            var divider = (int)Math.Pow(10, i);
            var minDigit = (min / divider) % 10;
            var maxDigit = (max / divider) % 10;

            var (from, to) = prevDiff switch
            {
                FillingSituation.AtTop => (0, maxDigit),
                FillingSituation.AtBottom => (minDigit, 9),
                FillingSituation.NarrowOptions => (maxDigit, minDigit),
                FillingSituation.NarrowOptionsOverflowing => (minDigit, maxDigit),
                FillingSituation.TopAndBottomAreSame => (minDigit, maxDigit),
                FillingSituation.Middle => (0, 9),
                _ => (0, 9),
            };

            var at = length - 1 - i;

            bool isFinish = at == slotIndex;

            if (isFinish)
            {
                if (from <= to)
                {
                    result = [.. Enumerable.Range(from, (to - from + 1)).Select(ToChar)];
                }
                else
                {
                    result =
                    [
                        .. Enumerable.Range(from, 10 - from).Select(ToChar),
                        .. Enumerable.Range(0, to + 1).Select(ToChar),
                    ];
                }
            }

            var currentChar = currentFilling[at];

            if (from == to)
            {
                prevDiff = FillingSituation.TopAndBottomAreSame;
            }
            else if (char.IsDigit(currentChar))
            {
                var current = ToNumber(currentChar);

                prevDiff =
                    current == from ? FillingSituation.AtBottom
                    : current == to ? FillingSituation.AtTop
                    : FillingSituation.Middle;
            }
            else
            {
                var diff = (
                    prevDiff == FillingSituation.NarrowOptions ? (from - to + 10) : (to - from)
                );
                prevDiff = diff switch
                {
                    0 => FillingSituation.TopAndBottomAreSame,
                    1 => minDigit > maxDigit
                        ? FillingSituation.NarrowOptionsOverflowing
                        : FillingSituation.NarrowOptions,
                    _ => FillingSituation.Middle,
                };
            }
        }

        return result ?? throw new InvalidOperationException();
    }

    public static char[][] ToCharOptionsOld(
        int minValue,
        int maxValue,
        byte length,
        ReadOnlySpan<char> currentFilling
    )
    {
        char[][] result = new char[length][];

        var prevDiff = FillingSituation.TopAndBottomAreSame;

        for (int i = length - 1; i >= 0; i--)
        {
            var divider = (int)Math.Pow(10, i);
            var minDigit = (minValue / divider) % 10;
            var maxDigit = (maxValue / divider) % 10;

            var (from, to) = prevDiff switch
            {
                FillingSituation.AtTop => (0, maxDigit),
                FillingSituation.AtBottom => (minDigit, 9),
                FillingSituation.NarrowOptions => (maxDigit, minDigit),
                FillingSituation.TopAndBottomAreSame => (minDigit, maxDigit),
                FillingSituation.Middle => (0, 9),
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
                prevDiff = FillingSituation.TopAndBottomAreSame;
            }
            else if (char.IsDigit(currentChar))
            {
                var current = ToNumber(currentChar);

                prevDiff =
                    current == from ? FillingSituation.AtBottom
                    : current == to ? FillingSituation.AtTop
                    : FillingSituation.Middle;
            }
            else
            {
                prevDiff =
                    to
                    - from switch
                    {
                        0 => FillingSituation.TopAndBottomAreSame,
                        1 => FillingSituation.NarrowOptions,
                        _ => FillingSituation.Middle,
                    };
            }
        }
        return result;
    }

    public static byte ToNumber(this char digit) => (byte)(digit - '0');

    public static char ToChar(this int digit) => (char)((digit % 10) + '0');

    public static char ToChar(this long digit) => (char)((digit % 10) + '0');

    public static IEnumerable<byte?> ToDigits(IEnumerable<char> chars) =>
        chars.Select(c => char.IsDigit(c) ? (byte?)ToNumber(c) : null);

    static char[] DigitSpan(char from, char to) => DigitSpan(ToNumber(from), ToNumber(to));

    static char[] DigitSpan(byte from, byte to) =>
        (from <= to)
            ? [.. Enumerable.Range(from, to - from + 1).Select(ToChar)]
            :
            [
                .. Enumerable.Range(from, 10 - (from - to)).Select(ToChar),
                .. Enumerable.Range(0, to + 1).Select(ToChar),
            ];

    enum FillingSituation
    {
        AtBottom = 1,
        NarrowOptions = 2,
        TopAndBottomAreSame = 3,
        AtTop = 4,
        Middle = 5,
        NarrowOptionsOverflowing = 6,
    }
}
