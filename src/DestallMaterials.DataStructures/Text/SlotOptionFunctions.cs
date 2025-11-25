using System;

namespace DestallMaterials.WheelProtection.DataStructures.Text;

public static class SlotOptionFunctions
{
    record Constraint(char[] Options, char[] PossibleDigits, byte? Selected);

    enum LeftToRightDigitConstraint
    {
        Free,
        WithinLowerTen,
        WithinUpperTen,
        WithinTwoTens,
        WithinSingleTen,
    }

    enum RightToLeftDigitConstraint
    {
        Free,
        ExcludeLower,
        ExcludeUpper,
    }

    static char[] GetOptionsForSlot_V3(
        byte slotIndex,
        ReadOnlySpan<char> currentFilling,
        long min,
        long max
    )
    {
        var thisDivider = (long)Math.Pow(10, currentFilling.Length - slotIndex - 1);

        var minDigit = (byte)((min / thisDivider) % 10);
        var maxDigit = (byte)((max / thisDivider) % 10);

        var result = slotIndex == 0 ? DigitSpan(minDigit, maxDigit) : DigitSpan(0, 9);

        LeftToRightDigitConstraint leftConstraint =
            slotIndex == 0
                ? LeftToRightDigitConstraint.WithinSingleTen
                : GetConstraintForNextDigit((byte)(slotIndex - 1), [.. currentFilling], min, max);

        RightToLeftDigitConstraint rightConstraint =
            slotIndex == currentFilling.Length - 1
                ? RightToLeftDigitConstraint.Free
                : GetConstraintForPreviousDigit(
                    (byte)(slotIndex + 1),
                    [.. currentFilling],
                    min,
                    max
                );

        result = leftConstraint switch
        {
            LeftToRightDigitConstraint.WithinLowerTen => DigitSpan(minDigit, 9),
            LeftToRightDigitConstraint.WithinUpperTen => DigitSpan(0, maxDigit),
            LeftToRightDigitConstraint.WithinSingleTen => DigitSpan(minDigit, maxDigit),
            LeftToRightDigitConstraint.WithinTwoTens when minDigit > maxDigit =>
            [
                .. DigitSpan(minDigit, maxDigit),
            ],
            _ => result,
        };

        if (result.Length > 1)
        {
            result = rightConstraint switch
            {
                RightToLeftDigitConstraint.ExcludeLower => result[1..],
                RightToLeftDigitConstraint.ExcludeUpper => result[..^1],
                _ => result,
            };
        }

        return result;
    }

    static RightToLeftDigitConstraint GetConstraintForPreviousDigit(
        byte slotIndex,
        ReadOnlySpan<char> currentFilling,
        long min,
        long max
    )
    {
        var thisDivider = (long)Math.Pow(10, currentFilling.Length - slotIndex - 1);

        var minDigit = (byte)((min / thisDivider) % 10);
        var maxDigit = (byte)((max / thisDivider) % 10);

        var currentChar = currentFilling[slotIndex];

        var currentValue = char.IsDigit(currentChar) ? (byte?)ToNumber(currentChar) : null;

        var constraintFromPrePrevious =
            slotIndex + 2 >= currentFilling.Length
                ? LeftToRightDigitConstraint.WithinSingleTen
                : GetConstraintForNextDigit((byte)(slotIndex + 2), currentFilling, min, max);

        if (constraintFromPrePrevious == LeftToRightDigitConstraint.Free)
        {
            return RightToLeftDigitConstraint.Free;
        }

        RightToLeftDigitConstraint result;
        if (currentValue is not null)
        {
            result = constraintFromPrePrevious switch
            {
                LeftToRightDigitConstraint.WithinLowerTen
                or LeftToRightDigitConstraint.WithinSingleTen => currentValue < minDigit
                    ? RightToLeftDigitConstraint.ExcludeLower
                    : RightToLeftDigitConstraint.Free,
                LeftToRightDigitConstraint.WithinUpperTen
                or LeftToRightDigitConstraint.WithinSingleTen => currentValue > maxDigit
                    ? RightToLeftDigitConstraint.ExcludeUpper
                    : RightToLeftDigitConstraint.Free,
                _ => RightToLeftDigitConstraint.Free,
            };

            return result;
        }

        if (maxDigit - minDigit >= 2)
        {
            return RightToLeftDigitConstraint.Free;
        }

        var fromNext =
            slotIndex < currentFilling.Length - 1
                ? GetConstraintForPreviousDigit((byte)(slotIndex + 1), currentFilling, min, max)
                : RightToLeftDigitConstraint.Free;

        result = fromNext switch
        {
            RightToLeftDigitConstraint.ExcludeLower => currentValue is null
                ? RightToLeftDigitConstraint.ExcludeLower
                : RightToLeftDigitConstraint.Free,
            RightToLeftDigitConstraint.ExcludeUpper => currentValue is null
                ? RightToLeftDigitConstraint.ExcludeUpper
                : RightToLeftDigitConstraint.Free,
            RightToLeftDigitConstraint.Free => RightToLeftDigitConstraint.Free,
            _ => RightToLeftDigitConstraint.Free,
        };

        return result;
    }

    static LeftToRightDigitConstraint GetConstraintForNextDigit(
        byte slotIndex,
        ReadOnlySpan<char> currentFilling,
        long min,
        long max
    )
    {
        var length = (byte)currentFilling.Length;
        var thisDivider = (long)Math.Pow(10, length - slotIndex - 1);

        var minDigit = (byte)((min / thisDivider) % 10);
        var maxDigit = (byte)((max / thisDivider) % 10);

        var currentChar = currentFilling[slotIndex];

        var currentValue = char.IsDigit(currentChar) ? (byte?)ToNumber(currentChar) : null;

        //if (slotIndex == 0)
        //{
        //    result = currentValue switch
        //    {
        //        null => (maxDigit - minDigit) switch
        //        {
        //            > 1 => DigitConstraint.Free,
        //            1 => DigitConstraint.WithinTwoTens,
        //            0 => DigitConstraint.WithinSingleTen,
        //            _ => DigitConstraint.Free,
        //        },
        //        _ when currentValue == maxDigit && currentValue == minDigit =>
        //            DigitConstraint.WithinSingleTen,
        //        _ when currentValue == minDigit => DigitConstraint.WithinLowerTen,
        //        _ when currentValue == maxDigit => DigitConstraint.WithinUpperTen,
        //        _ => DigitConstraint.Free,
        //    };

        //    return result;
        //}


        var prevConstraint =
            slotIndex == 0
                ? LeftToRightDigitConstraint.WithinSingleTen
                : GetConstraintForNextDigit((byte)(slotIndex - 1), currentFilling, min, max);

        var resultSpanLength =
            maxDigit < minDigit ? maxDigit + 10 - minDigit + 1 : maxDigit - minDigit + 1;

        var result = prevConstraint switch
        {
            LeftToRightDigitConstraint.WithinLowerTen when currentValue == minDigit =>
                LeftToRightDigitConstraint.WithinLowerTen,
            LeftToRightDigitConstraint.WithinUpperTen when currentValue == maxDigit =>
                LeftToRightDigitConstraint.WithinUpperTen,
            LeftToRightDigitConstraint.WithinTwoTens when currentValue == maxDigit =>
                LeftToRightDigitConstraint.WithinUpperTen,
            LeftToRightDigitConstraint.WithinTwoTens when currentValue == minDigit =>
                LeftToRightDigitConstraint.WithinLowerTen,
            LeftToRightDigitConstraint.WithinSingleTen when minDigit == maxDigit =>
                LeftToRightDigitConstraint.WithinSingleTen,
            LeftToRightDigitConstraint.WithinSingleTen when currentValue == maxDigit =>
                LeftToRightDigitConstraint.WithinUpperTen,
            LeftToRightDigitConstraint.WithinSingleTen when currentValue == minDigit =>
                LeftToRightDigitConstraint.WithinLowerTen,
            not LeftToRightDigitConstraint.Free when resultSpanLength < 3 =>
                LeftToRightDigitConstraint.WithinTwoTens,
            _ => LeftToRightDigitConstraint.Free,
        };

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

        //if (filling.All(c => c != default) && long.TryParse(filling, out var definiteNumber))
        //{
        //    return GetOptionsForDefiniteNumber(slotIndex, definiteNumber, length, min, max);
        //}

        var divider = (int)Math.Pow(10, length - 1 - slotIndex);

        Constraint[] constraints = new Constraint[length];

        foreach (var (digit, index) in ToDigits(filling).Select((d, i) => (d, i)))
        {
            var thisDivider = (long)Math.Pow(10, length - index - 1);

            var isTargetSlot = index == slotIndex;

            var minDigit = (byte)((min / thisDivider) % 10);
            var maxDigit = (byte)((max / thisDivider) % 10);

            if (index == 0)
            {
                if (minDigit > maxDigit)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(min),
                        $"{nameof(min)} > {nameof(max)}"
                    );
                }
                var options = DigitSpan(minDigit, maxDigit);
                constraints[index] = new(
                    options,
                    DigitSpan(minDigit, maxDigit),
                    index == slotIndex ? null : digit
                );

                continue;
            }

            var (prevMinMax, prevOptions, prevSelected) = constraints[index - 1];

            char[] thisConstraint;
            if (prevSelected is null)
            {
                thisConstraint = prevMinMax.Length switch
                {
                    0 => [],
                    1 => DigitSpan(minDigit, maxDigit),
                    2 => maxDigit > minDigit ? DigitSpan(0, 9) : DigitSpan(minDigit, maxDigit),
                    _ => DigitSpan(0, 9),
                };
            }
            else
            {
                thisConstraint = prevMinMax.Length switch
                {
                    0 => [],
                    1 => DigitSpan(minDigit, maxDigit),
                    2 => prevSelected == prevMinMax[0]
                        ? DigitSpan(minDigit, 9)
                        : DigitSpan(0, maxDigit),
                    _ => DigitSpan(0, 9),
                };
            }

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

            constraints[index] = new(
                thisConstraint,
                DigitSpan(minDigit, maxDigit),
                index == slotIndex ? null : digit
            );
        }

        for (var i = length - 1; i >= 2; i--)
        {
            var (_, constraint, selected) = constraints[i];
            if (constraint.Length == 0)
            {
                return [];
            }

            var (_, prePrevConstraint, prePrevSelected) = constraints[i - 2];

            if (constraint.Length > 1 || prePrevConstraint.Length > 2)
            {
                continue;
            }

            var digit = ToNumber(constraint[0]);

            var thisDivider = (long)Math.Pow(10, length - i - 1);

            var minDigit = (byte)((min / thisDivider) % 10);
            var maxDigit = (byte)((max / thisDivider) % 10);

            var (prevOptions, prevPossibleDigits, prevSelected) = constraints[i - 1];

            prevPossibleDigits =
                (digit > maxDigit && digit < minDigit) ? prevPossibleDigits[1..^1]
                : digit < minDigit ? prevPossibleDigits[1..]
                : digit > maxDigit ? prevPossibleDigits[..^1]
                : prevPossibleDigits;

            constraints[i - 1] = new(prevOptions, prevPossibleDigits, prevSelected);
        }

        var result = constraints[slotIndex];

        return [.. result.Options.Distinct()];
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

       return GetOptionsForSlot_V3(slotIndex, currentFilling, min, max);
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
            : [.. DigitSpan(from, 9), .. DigitSpan(0, to)];

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
