using EverModern.WheelProtection.DataStructures.Text;

namespace EverModern.WheelProtection.DataStructures.Text;

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
        Impossible,
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

        LeftToRightDigitConstraint constraintFromLeft =
            slotIndex == 0
                ? LeftToRightDigitConstraint.WithinSingleTen
                : GetConstraintForNextDigit((byte)(slotIndex - 1), [.. currentFilling], min, max);

        if (constraintFromLeft == LeftToRightDigitConstraint.Impossible)
        {
            return [];
        }

        RightToLeftDigitConstraint constraintFromRight =
            slotIndex == currentFilling.Length - 1
                ? RightToLeftDigitConstraint.Free
                : GetConstraintForPreviousDigit(
                    (byte)(slotIndex + 1),
                    [.. currentFilling],
                    min,
                    max
                );

        result = constraintFromLeft switch
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
            result = constraintFromRight switch
            {
                RightToLeftDigitConstraint.ExcludeLower => RemoveFromBottom(
                    result,
                    ToChar(minDigit)
                ),
                RightToLeftDigitConstraint.ExcludeUpper => RemoveFromTop(result, ToChar(maxDigit)),
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
        if (slotIndex == 0)
        {
            return RightToLeftDigitConstraint.Free;
        }

        var thisDivider = (long)Math.Pow(10, currentFilling.Length - slotIndex - 1);

        var minDigit = (byte)((min / thisDivider) % 10);
        var maxDigit = (byte)((max / thisDivider) % 10);

        var currentChar = currentFilling[slotIndex];

        var currentValue = char.IsDigit(currentChar) ? (byte?)ToNumber(currentChar) : null;

        var constraintFromPrePrevious =
            slotIndex == 1
                ? LeftToRightDigitConstraint.WithinSingleTen
                : GetConstraintForNextDigit((byte)(slotIndex - 2), currentFilling, min, max);

        if (constraintFromPrePrevious == LeftToRightDigitConstraint.Free)
        {
            return RightToLeftDigitConstraint.Free;
        }

        RightToLeftDigitConstraint result;
        if (currentValue is not null)
        {
            var isInTopPart = currentValue <= maxDigit;
            var isInBottomPart = currentValue >= minDigit;
            result = constraintFromPrePrevious switch
            {
                LeftToRightDigitConstraint.WithinLowerTen
                or LeftToRightDigitConstraint.WithinSingleTen
                or LeftToRightDigitConstraint.WithinTwoTens when isInBottomPart == false =>
                    RightToLeftDigitConstraint.ExcludeLower,
                LeftToRightDigitConstraint.WithinUpperTen
                or LeftToRightDigitConstraint.WithinSingleTen
                or LeftToRightDigitConstraint.WithinTwoTens when isInTopPart == false =>
                    RightToLeftDigitConstraint.ExcludeUpper,
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

        var prevConstraint =
            slotIndex == 0
                ? LeftToRightDigitConstraint.WithinSingleTen
                : GetConstraintForNextDigit((byte)(slotIndex - 1), currentFilling, min, max);

        if (prevConstraint == LeftToRightDigitConstraint.Impossible)
        {
            return LeftToRightDigitConstraint.Impossible;
        }

        if (currentValue is not null)
        {
            var makesImpossible = prevConstraint switch
            {
                LeftToRightDigitConstraint.WithinLowerTen => currentValue < minDigit,
                LeftToRightDigitConstraint.WithinUpperTen => currentValue > maxDigit,
                LeftToRightDigitConstraint.WithinTwoTens => DigitSpan(minDigit, maxDigit)
                    .Contains(ToChar(currentValue.Value)),
                LeftToRightDigitConstraint.WithinSingleTen => currentValue < minDigit
                    || currentValue > maxDigit,
                _ => false,
            };

            if (makesImpossible)
            {
                return LeftToRightDigitConstraint.Impossible;
            }
        }

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

    public static byte ToNumber(this char digit) => (byte)(digit - '0');

    public static char ToChar(this int digit) => (char)((digit % 10) + '0');

    static char[] DigitSpan(byte from, byte to) =>
        (from <= to)
            ? [.. Enumerable.Range(from, to - from + 1).Select(ToChar)]
            : [.. DigitSpan(from, 9), .. DigitSpan(0, to)];

    static char[] RemoveFromBottom(char[] chars, char value)
    {
        if (chars[0] == value)
        {
            return chars[1..];
        }

        return chars;
    }

    static char[] RemoveFromTop(char[] chars, char value)
    {
        if (chars[^1] == value)
        {
            return chars[..^1];
        }

        return chars;
    }

    internal static bool Contains(this Range range, int value) =>
        range.Start.Value <= value && value < range.End.Value;
}
