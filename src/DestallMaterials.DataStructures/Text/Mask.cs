namespace DestallMaterials.WheelProtection.DataStructures.Text;

public class Mask<TSymbol> : IMask<TSymbol>
    where TSymbol : struct
{
    readonly ISlotConstraintsSource<TSymbol> _constraintsSource;
    readonly IEqualityComparer<TSymbol?> _equalityComparer;
    readonly TSymbol?[] _slots;

    public Mask(
        ISlotConstraintsSource<TSymbol> constraintsSource,
        IReadOnlyList<TSymbol?> initialSlots,
        IEqualityComparer<TSymbol?> equalityComparer
    )
    {
        _constraintsSource =
            constraintsSource ?? throw new ArgumentNullException(nameof(constraintsSource));
        _equalityComparer = equalityComparer ?? EqualityComparer<TSymbol?>.Default;
        _slots = initialSlots == null ? new TSymbol?[_constraintsSource.Length] : [.. initialSlots];
    }

    public IReadOnlyList<TSymbol?> Slots => _slots;

    IReadOnlyList<SlotConstraint<TSymbol>> Constraints => _constraintsSource.GetConstraints(_slots);

    public int AcceptChange(ContentChange<TSymbol?> contentChange)
    {
        var (at, removed, inserted) = contentChange;

        if (at > _slots.Length)
            throw new InvalidOperationException("Can't change beyond slots count.");

        // Clamp indices
        at = Math.Clamp(at, 0, _slots.Length);

        // Process removals: clear slots that were removed (starting at 'at' and moving left)
        for (int i = 0; i < removed; i++)
        {
            var idx = at - i;
            if (idx < 0 || idx >= _slots.Length)
                continue;

            var options = Constraints[idx].Options;
            _slots[idx] = options.Count == 1 ? options[0] : default;
        }

        // After removals, run autoset to fill deterministic slots
        AutosetAll();

        // Determine insertion start position: if insertion at end (at == length) start at last slot
        int insertPos = Math.Min(at, Math.Max(0, _slots.Length - 1));

        // If removals removed everything up to and including the first slot, place at0
        if (at - removed <= 0)
            insertPos = 0;

        int placedPos = insertPos;

        // Process insertions sequentially
        for (int i = 0; i < inserted.Length && placedPos < _slots.Length; )
        {
            var options = Constraints[placedPos].Options;
            var value = inserted[i];

            if (options.Count == 0)
            {
                // slot can't accept anything: clear and advance
                _slots[placedPos] = default;
                placedPos++;
                continue;
            }

            if (options.Count == 1)
            {
                // deterministic slot - fill and advance, but do not consume inserted value
                _slots[placedPos] = options[0];
                placedPos++;
                continue;
            }

            // slot accepts multiple values
            if (options.Contains(value))
            {
                _slots[placedPos] = value;
                i++; // consume inserted
                placedPos++;
                AutosetAll();
                continue;
            }

            // value not acceptable here -> try next slot
            placedPos++;
        }

        // If there were no insertions, compute caret after deletion
        if (inserted.Length == 0)
        {
            var caret = at - removed;
            if (caret < 0)
                caret = 0;
            return caret;
        }

        // Caret after insertions: last placed position
        var caretPos = Math.Clamp(placedPos - 1, 0, Math.Max(0, _slots.Length - 1));
        return caretPos;
    }

    bool AutosetAll()
    {
        var changed = false;

        // Repeat until stable or safety limit
        for (int iteration = 0; iteration < 32; iteration++)
        {
            var currentConstraints = Constraints;
            bool anyChange = false;

            for (int i = 0; i < _slots.Length; i++)
            {
                var slotValue = _slots[i];
                var options = currentConstraints[i].Options;
                TSymbol? newValue = slotValue;

                if (options.Count == 1)
                {
                    newValue = options[0];
                }
                else if (options.Count == 0)
                {
                    newValue = default;
                }
                else if (slotValue is not null && !options.Contains(slotValue))
                {
                    // choose a deterministic fallback (last option)
                    newValue = options[options.Count - 1];
                }

                if (!_equalityComparer.Equals(newValue, slotValue))
                {
                    _slots[i] = newValue;
                    anyChange = true;
                }
            }

            changed = changed || anyChange;
            if (!anyChange)
                break;
        }

        return changed;
    }
}
