using System.Runtime.InteropServices;

namespace DestallMaterials.WheelProtection.DataStructures.Text;

public static class SlotOptionFunctions
{
    static IEnumerable<long> LongSeq(long min, long max, long pace)
    {
        for (long option = min; option <= max; option += pace)
        {
            yield return option;
        }
    }

    static char[] 

    static char[] GetOptionsForSlotReliable(
        byte slotIndex,
        ReadOnlySpan<char> currentFilling,
        byte length,
        long min,
        long max
    )
    {
        char[] filling = [.. currentFilling];

        var divider = (int)Math.Pow(10, length - 1 - slotIndex);

        var pace = 

        var result = LongSeq(min, max)
            .Where(option => IsGoodOption(option, slotIndex, filling, length))
            .Select(option => ToChar(option / divider % 10))
            .Distinct()
            .ToArray();

        return result;
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
