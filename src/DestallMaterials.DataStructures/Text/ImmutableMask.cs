using System.Collections;

namespace DestallMaterials.WheelProtection.DataStructures.Text;

public class ImmutableMask<TSymbol> : IImmutableMask<TSymbol, ImmutableMask<TSymbol>>
{
    readonly IReadOnlyList<TSymbol> _slots;
    readonly IEqualityComparer<TSymbol> _equalityComparer;
    readonly ISlotConstraintsSource<TSymbol> _constraintsSource;

    public ImmutableMask(
        IReadOnlyList<TSymbol> slots,
        ISlotConstraintsSource<TSymbol> constraintsSource,
        IEqualityComparer<TSymbol> equalityComparer
    )
    {
        _slots = [.. slots];
        _equalityComparer = equalityComparer;
        _constraintsSource = constraintsSource;
    }

    public ImmutableMask(
        IReadOnlyList<TSymbol> slots,
        ISlotConstraintsSource<TSymbol> constraintsSource
    )
    {
        _slots = [.. slots];
        _equalityComparer = EqualityComparer<TSymbol>.Default;
        _constraintsSource = constraintsSource;
    }

    public TSymbol this[int index] => _slots[index];

    public int Count => _slots.Count;

    public ImmutableMask<TSymbol> Change(
        ContentChange<TSymbol> contentChange,
        out int caretPosition
    )
    {
        var (at, removed, inserted) = contentChange;
        var length = _slots.Count;

        if (at > _slots.Count)
            throw new InvalidOperationException("Can't change beyond slots count.");

        TSymbol[] slots = [.. _slots];

        var tryingSlots = slots.ToArray();

        for (int i = 0; i < removed; i++)
        {
            var idx = at + i;
            if (idx < 0 || idx >= length)
                continue;

            var options = GetConstraints(slots, idx).Options;
            tryingSlots[idx] = options.Count >= 1 ? options[0] : default!;
        }

        void AutosetOther(int nowAt)
        {
            if (at > 0)
            {
                Autoset(tryingSlots, 0, at - 1, true);
            }
            if (nowAt < length)
            {
                Autoset(tryingSlots, nowAt, length - 1, true);
            }
        }

        AutosetOther(Math.Min(length - 1, at + removed));

        if (inserted.Length == 0)
        {
            caretPosition = at;
            return new(slots, _constraintsSource, _equalityComparer);
        }

        var optionsToCheck = GetConstraints(slots, at).Options;
        var fillsExisting = optionsToCheck.Contains(inserted[0], _equalityComparer);

        caretPosition = at;

        var fill = ForceInsert;

        if (fillsExisting)
        {
            fill = SoftInsert;
        }

        foreach (var insertedSymbol in inserted)
        {
            var oneSuccess = fill(tryingSlots, insertedSymbol, caretPosition, out caretPosition);
            if (oneSuccess is false)
            {
                return new(slots, _constraintsSource, _equalityComparer);
            }
            AutosetOther(caretPosition);
        }

        return new(tryingSlots, _constraintsSource, _equalityComparer);
    }

    public IEnumerator<TSymbol> GetEnumerator() => ((IEnumerable<TSymbol>)_slots).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    SlotConstraint<TSymbol> GetConstraints(TSymbol[] slots, int slotIndex) =>
        _constraintsSource.GetSlotConstraints(slotIndex, slots);

    bool SoftInsert(Span<TSymbol> slots, TSymbol inserted, int at, out int caretPosition)
    {
        var options = GetConstraints([.. slots], at).Options;

        if (options.Contains(inserted) is false)
        {
            caretPosition = at;
            return false;
        }

        slots[at] = inserted;
        caretPosition = at + 1;
        return true;
    }

    bool ForceInsert(Span<TSymbol> slots, TSymbol inserted, int at, out int caretPosition)
    {
        var value = inserted;
        slots[at] = value;

        var autosetSlots = slots.ToArray();

        var (autosetRight, hadToForce, _) =
            at > 0
                ? Autoset(autosetSlots, at - 1, 0, allowOriginalValues: true)
                : new(true, false, at);

        var (autosetLeft, hadToForceLeft, _) = autosetRight is true
            ? at < slots.Length - 1
                ? Autoset(autosetSlots, at + 1, slots.Length - 1, hadToForce)
                : new(true, false, at)
            : new(false, false, at);

        at++;
        caretPosition = at;

        if (autosetLeft)
        {
            slots[at] = value;
            return true;
        }

        if (at >= slots.Length - 1)
        {
            return false;
        }

        var tryPushFurther = ForceInsert(slots, inserted, at, out caretPosition);

        return tryPushFurther;
    }

    AutosetResult Autoset(TSymbol[] slots, int from, int to, bool allowOriginalValues)
    {
        bool directionRight = to > from;
        for (
            int i = from;
            (directionRight ? to - i : i - from) >= 0 && i < slots.Length && i >= 0;
            i = directionRight ? i + 1 : i - 1
        )
        {
            var varL = Math.Abs(i - to);
            TSymbol[] variableSlots = Replaced(slots, i, to, default);

            var options = GetConstraints(variableSlots, i).Options;
            if (options.Count == 0)
            {
                return new(false, allowOriginalValues is false, i);
            }
            allowOriginalValues =
                allowOriginalValues && options.Contains(slots[i], _equalityComparer);
            if (allowOriginalValues is false)
            {
                slots[i] = options[0];
            }
        }

        return new(true, allowOriginalValues, to);
    }

    static TSymbol[] Replaced(TSymbol[] source, int from, int to, TSymbol with)
    {
        TSymbol[] result = [.. source];

        if (to < from)
        {
            (to, from) = (from, to);
        }

        for (int i = from; i <= to && i < source.Length; i++)
        {
            result[i] = with;
        }

        return result;
    }

    record struct AutosetResult(bool ManagedAutoset, bool HadToForce, int LastIndex);
}
