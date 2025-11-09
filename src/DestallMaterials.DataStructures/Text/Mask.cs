namespace DestallMaterials.WheelProtection.DataStructures.Text;

public class Mask<TSymbol> : IMask<TSymbol>
    where TSymbol : struct
{
    readonly ISlotConstraintsSource<TSymbol> _constraintsSource;
    readonly IEqualityComparer<TSymbol> _equalityComparer;
    readonly TSymbol[] _slots;

    public Mask(
        ISlotConstraintsSource<TSymbol> constraintsSource,
        IReadOnlyList<TSymbol> initialSlots,
        IEqualityComparer<TSymbol> equalityComparer
    )
    {
        _constraintsSource =
            constraintsSource ?? throw new ArgumentNullException(nameof(constraintsSource));
        _equalityComparer = equalityComparer ?? EqualityComparer<TSymbol>.Default;
        _slots = initialSlots == null ? new TSymbol[_constraintsSource.Length] : [.. initialSlots];
    }

    public IReadOnlyList<TSymbol> Slots => _slots;

    IReadOnlyList<SlotConstraint<TSymbol>> Constraints => _constraintsSource.GetConstraints(_slots);

    public int AcceptChange(ContentChange<TSymbol> contentChange)
    {
        var (at, removed, inserted) = contentChange;

        if (at > _slots.Length)
            throw new InvalidOperationException("Can't change beyond slots count.");

        // Clamp indices
        at = Math.Clamp(at, 0, _slots.Length);

        // Process removals: clear slots that were removed (starting at 'at' and moving right)
        for (int i = 0; i < removed; i++)
        {
            var idx = at + i;
            if (idx < 0 || idx >= _slots.Length)
                continue;

            var options = Constraints[idx].Options;
            _slots[idx] = options.Count >= 1 ? options[0] : default;
        }

        // After removals, run autoset to fill deterministic slots
        AutosetAll();

        // Determine insertion start position: start at 'at'. At may equal _slots.Length (append position)
        int placedPos = Math.Clamp(at, 0, _slots.Length);

        // Process insertions sequentially with a source index so we can handle paste intelligently
        int srcIndex = 0;
        while (srcIndex < inserted.Length && placedPos < _slots.Length)
        {
            var options = Constraints[placedPos].Options;
            var value = inserted[srcIndex];

            if (options.Count == 0)
            {
                // slot can't accept anything: clear and advance
                _slots[placedPos] = default;
                placedPos++;
                continue;
            }

            if (options.Count == 1)
            {
                // deterministic slot - fill and advance
                var deterministic = options[0];
                _slots[placedPos] = deterministic;

                // If the next inserted value equals the deterministic option, consume it as well
                if (srcIndex < inserted.Length && _equalityComparer.Equals(deterministic, value))
                {
                    srcIndex++;
                }

                placedPos++;
                continue;
            }

            // slot accepts multiple values
            if (options.Contains(value))
            {
                _slots[placedPos] = value;
                srcIndex++; // consume inserted
                placedPos++;

                // Allow autoset to propagate deterministic fills between insertions
                AutosetAll();
                continue;
            }

            // value not acceptable here -> try next slot
            placedPos++;
        }

        // If there were no insertions consumed, treat as deletion/no-op and return caret at 'at'
        if (srcIndex == 0)
        {
            var caret = Math.Clamp(at, 0, _slots.Length);
            return caret;
        }

        // Caret after insertions: placedPos is the index after last placed slot and may equal _slots.Length
        var caretAfterInsert = Math.Clamp(placedPos, 0, _slots.Length);
        return caretAfterInsert;
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
                TSymbol newValue = slotValue;

                if (options.Count == 0)
                {
                    throw new InvalidOperationException($"Options for slot {i} are empty.");
                }

                if (options.Count == 1)
                {
                    newValue = options[0];
                }
                else if (slotValue.Equals(options[0]) && !options.Contains(slotValue))
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
