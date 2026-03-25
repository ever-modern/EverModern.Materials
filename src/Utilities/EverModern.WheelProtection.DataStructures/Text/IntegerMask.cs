using System.Collections;
using Number = System.Int64;

namespace EverModern.WheelProtection.DataStructures.Text;

/// <summary>
/// Immutable mask for integer editing.
/// </summary>
public class IntegerMask(Number Value, Number MinValue, Number MaxValue, byte Length)
    : IImmutableMask<char, IntegerMask>
{
    readonly string _valueString = Value.ToString().PadLeft(Length, '0');

    /// <inheritdoc />
    public char this[int index] => _valueString[index];

    /// <inheritdoc />
    public int Count => Length;

    /// <summary>
    /// Gets the current numeric value.
    /// </summary>
    public Number Value => Value;

    /// <inheritdoc />
    public IntegerMask Change(ContentChange<char> contentChange, out int caretPosition)
    {
        var (at, removed, inserted) = contentChange;

        Span<char> slots = [.. _valueString];

        for (int i = 0; i < removed; i++)
        {
            var idx = at + i;
            if (idx < 0 || idx >= Length)
                continue;

            var options = GetOptionsForSlot(slots, idx);

            slots[idx] = options.Length >= 1 ? options[0] : '0';
        }

        if (inserted.Length == 0)
        {
            caretPosition = SkipNoOptionsLeft(slots, at - 1);
            var result = Number.Parse(slots);
            result = Math.Clamp(result, MinValue, MaxValue);
            return New(result);
        }

        if (inserted.Length == 1)
        {
            var insertedSymbol = inserted[0];
            for (; at < Length; at++)
            {
                var candidate = slots.ToArray();
                candidate[at] = insertedSymbol;

                var candidateValue = Number.Parse(candidate);

                if (candidateValue >= MinValue && candidateValue <= MaxValue)
                {
                    caretPosition = SkipNoOptionsRight(slots, at + 1);
                    return New(candidateValue);
                }

                candidateValue = Math.Clamp(candidateValue, MinValue, MaxValue);
                var thisDivider = (Number)Math.Pow(10, Length - at - 1);
                if (candidateValue / thisDivider % 10 + '0' == insertedSymbol)
                {
                    caretPosition = SkipNoOptionsRight(slots, at + 1);
                    return New(candidateValue);
                }
            }

            caretPosition = contentChange.At;
            return this;
        }

        inserted.CopyTo(slots[at..(at + inserted.Length)]);

        var resultValue = Number.Parse(slots);

        if (resultValue <= MaxValue && resultValue >= MinValue)
        {
            caretPosition = SkipNoOptionsRight(slots, at + inserted.Length);
            return New(resultValue);
        }

        caretPosition = contentChange.At;
        return this;
    }

    /// <inheritdoc />
    public IEnumerator<char> GetEnumerator() => _valueString.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    char[] GetOptionsForSlot(Span<char> currentFilling, int at) =>
        SlotOptionFunctions.GetOptionsForSlot((byte)at, currentFilling, Length, MinValue, MaxValue);

    IntegerMask New(Number newValue) => new(newValue, MinValue, MaxValue, Length);

    int SkipNoOptionsLeft(Span<char> slots, int from)
    {
        for (int i = from; i >= 0; i--)
        {
            var options = GetOptionsForSlot(slots, i);
            if (options.Length > 1)
            {
                return i;
            }
        }

        return 0;
    }

    int SkipNoOptionsRight(Span<char> slots, int from)
    {
        for (int i = from; i < Length; i++)
        {
            var options = GetOptionsForSlot(slots, i);
            if (options.Length > 1)
            {
                return i;
            }
        }

        return Length;
    }
}
