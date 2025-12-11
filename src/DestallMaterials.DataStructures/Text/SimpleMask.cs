using System.Collections;

namespace DestallMaterials.WheelProtection.DataStructures.Text;

public class SimpleMask<TSymbol> : IImmutableMask<TSymbol, SimpleMask<TSymbol>>
{
    readonly IReadOnlyList<TSymbol> _slots;
    readonly IEqualityComparer<TSymbol> _equalityComparer;
    readonly ISlotConstraintsSource<TSymbol> _constraintsSource;

    public SimpleMask(
        IReadOnlyList<TSymbol> slots,
        ISlotConstraintsSource<TSymbol> constraintsSource,
        IEqualityComparer<TSymbol> equalityComparer
    )
    {
        _slots = [.. slots];
        _equalityComparer = equalityComparer;
        _constraintsSource = constraintsSource;
    }

    public SimpleMask(
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

    public SimpleMask<TSymbol> Change(
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

        if (inserted.Length == 0)
        {
            AutosetOther(Math.Min(length - 1, at + removed));
            caretPosition = ReachAnyOptionsLeft(tryingSlots, at);
            return new(tryingSlots, _constraintsSource, _equalityComparer);
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

        caretPosition = ReachAnyOptionsRight(tryingSlots, caretPosition);

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

        var initialSlots = slots;

        slots = new TSymbol[slots.Length];
        initialSlots.CopyTo(slots);

        slots[at] = value;

        var autosetSlots = slots.ToArray();

        AutosetResult AutosetLeft(bool allowOriginalValues) =>
            at > 0 ? Autoset(autosetSlots, at - 1, 0, allowOriginalValues) : new(true, true, at);

        AutosetResult AutosetRight(bool allowOriginalValues) =>
            at < autosetSlots.Length - 1
                ? Autoset(autosetSlots, at + 1, autosetSlots.Length - 1, allowOriginalValues)
                : new(true, true, at);

        var autoset = AutosetLeft(true);
        if (autoset.Success is false)
        {
            autoset = AutosetRight(true);
            if (autoset.Success)
            {
                autoset = AutosetLeft(autoset.ValuesWereFit);
            }
        }
        else
        {
            autoset = AutosetRight(autoset.ValuesWereFit);
        }

        if (autoset.Success)
        {
            autosetSlots.CopyTo(initialSlots);
            caretPosition = at + 1;
            return true;
        }

        if (at >= slots.Length - 1)
        {
            caretPosition = at;
            return false;
        }

        initialSlots.CopyTo(slots);

        for (int i = at + 1; i < slots.Length; i++)
        {
            var nextSlotOptions = GetConstraints([.. slots], i).Options;
            if (nextSlotOptions.Contains(value))
            {
                caretPosition = i + 1;
                slots[i] = value;
                slots.CopyTo(initialSlots);
                return true;
            }
        }

        caretPosition = at;
        return false;
    }

    AutosetResult Autoset(TSymbol[] slots, int from, int to, bool allowOriginalValues)
    {
        bool directionRight = to > from;
        var initialSlots = slots;
        slots = [.. slots];
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

        slots.CopyTo(initialSlots, 0);
        return new(true, allowOriginalValues, to);
    }

    int ReachAnyOptionsRight(TSymbol[] slots, int from)
    {
        while (from < slots.Length && GetConstraints(slots, from).Options.Count <= 1)
        {
            from++;
        }

        return from;
    }

    int ReachAnyOptionsLeft(TSymbol[] slots, int from)
    {
        while (from > 0 && GetConstraints(slots, from - 1).Options.Count <= 1)
        {
            from--;
        }

        return from;
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

    record struct AutosetResult(bool Success, bool ValuesWereFit, int LastIndex);
}
