namespace DestallMaterials.WheelProtection.DataStructures.Text;

public class Mask<TSymbol> : IMask<TSymbol>
    where TSymbol : struct
{
    readonly ISlotConstraintsSource<TSymbol> _constraintsSource;
    readonly IEqualityComparer<TSymbol> _equalityComparer;
    volatile TSymbol[] _slots;

    public Mask(
        ISlotConstraintsSource<TSymbol> constraintsSource,
        IReadOnlyList<TSymbol> initialSlots,
        IEqualityComparer<TSymbol> equalityComparer
    )
    {
        _constraintsSource =
            constraintsSource ?? throw new ArgumentNullException(nameof(constraintsSource));
        _equalityComparer = equalityComparer;
        _slots = initialSlots == null ? new TSymbol[_constraintsSource.Length] : [.. initialSlots];
    }

    public Mask(
        ISlotConstraintsSource<TSymbol> constraintsSource,
        IReadOnlyList<TSymbol> initialSlots
    )
        : this(constraintsSource, initialSlots, EqualityComparer<TSymbol>.Default) { }

    public IReadOnlyList<TSymbol> Slots => _slots;

    SlotConstraint<TSymbol> GetConstraints(TSymbol[] slots, int slotIndex) =>
        _constraintsSource.GetSlotConstraints(slotIndex, slots);

    public int AcceptChange(ContentChange<TSymbol> contentChange) =>
        AcceptChange(contentChange, allowForce: true);

    int AcceptChange(ContentChange<TSymbol> contentChange, bool allowForce)
    {
        var (at, removed, inserted) = contentChange;

        if (at > _slots.Length)
            throw new InvalidOperationException("Can't change beyond slots count.");

        var oldSlots = _slots;

        TSymbol[] slots = [.. oldSlots];

        // Clamp indices
        at = Math.Clamp(at, 0, slots.Length);

        // Process removals: clear slots that were removed (starting at 'at' and moving right)
        for (int i = 0; i < removed; i++)
        {
            var idx = at + i;
            if (idx < 0 || idx >= slots.Length)
                continue;

            var options = GetConstraints(slots, idx).Options;
            slots[idx] = options.Count >= 1 ? options[0] : default;
        }

        // Determine insertion start position: start at 'at'. At may equal slots.Length (append position)
        int placedAt = Math.Clamp(at, 0, slots.Length);

        // Process insertions sequentially with a source index so we can handle paste intelligently
        int srcIndex = 0;

        while (srcIndex < inserted.Length && placedAt < slots.Length)
        {
            var options = GetConstraints(slots, placedAt).Options;
            var value = inserted[srcIndex];

            //if (options.Count == 0)
            //{
            //    return at; // slot cannot accept any value, treat as no-op
            //}

            // slot accepts multiple values
            if (options.Contains(value, _equalityComparer))
            {
                // When inserting without removal (pure insertion), we need to push the existing value
                // to a subsequent slot where it's allowed
                if (removed == 0 && srcIndex == 0)
                {
                    var existingValue = slots[placedAt];
                    // Push the existing value if slot has content (not default) or content differs from inserted
                    if (
                        !_equalityComparer.Equals(existingValue, default)
                        || !_equalityComparer.Equals(existingValue, value)
                    )
                    {
                        slots[placedAt] = value;

                        // Try to push the existing value to subsequent slots
                        bool pushedSuccessfully = false;
                        for (int pushPos = placedAt + 1; pushPos < slots.Length; pushPos++)
                        {
                            var pushOptions = GetConstraints(slots, pushPos).Options;
                            if (pushOptions.Contains(existingValue, _equalityComparer))
                            {
                                slots[pushPos] = existingValue;

                                // Autoset the slots between insertion and push position first
                                Autoset(slots, placedAt + 1, pushPos - 1);
                                // Then autoset remaining slots after push position
                                Autoset(slots, pushPos + 1, slots.Length - 1);

                                pushedSuccessfully = true;
                                // Caret goes after the pushed value; bounds checked by while loop condition
                                placedAt = pushPos + 1;
                                break;
                            }
                        }

                        if (pushedSuccessfully)
                        {
                            srcIndex++;
                            // Update slots and continue with next inserted value if any
                            _slots = slots;
                            continue;
                        }
                        else
                        {
                            // Could not push, revert the slot change
                            slots[placedAt] = existingValue;
                        }
                    }
                }

                slots[placedAt] = value;

                // Allow autoset to propagate deterministic fills between insertions
                Autoset(slots, placedAt + 1, slots.Length - 2);

                // value not acceptable here -> try next slot
                srcIndex++;
                placedAt++;
            }
            else
            {
                for (int j = placedAt; j < slots.Length; j++)
                {
                    slots[j] = value;

                    var autosetSlots = slots.ToArray();

                    var autosetRight =
                        j > 0 ? Autoset(autosetSlots, j - 1, 0, allowOriginalValues: true) : true;

                    var autosetLeft = autosetRight is not null
                        ? j < slots.Length - 1
                            ? Autoset(slots, j + 1, slots.Length - 1, autosetRight.Value)
                            : true
                        : null;

                    slots = autosetLeft is not null ? autosetSlots : slots;

                    if (autosetLeft is null or false)
                    {
                        if (j == slots.Length - 1)
                        {
                            return at;
                        }
                        else
                        {
                            slots[j] = _slots[j];
                        }
                    }
                    else
                    {
                        placedAt = j + 1;

                        if (srcIndex == 0 && allowForce)
                        {
                            var theseSlots = _slots;
                            _slots = new TSymbol[_slots.Length];
                            var recResult = AcceptChange(contentChange, false);
                            bool managedToChange = recResult > at;
                            if (managedToChange is false)
                            {
                                _slots = theseSlots;
                                return at;
                            }

                            return recResult;
                        }

                        return at + srcIndex;
                    }
                }
            }
        }

        Autoset(slots, 0, slots.Length - 1);

        _slots = slots;

        // If there were no insertions consumed, treat as deletion/no-op and return caret at 'at'
        if (srcIndex == 0)
        {
            var caret = Math.Clamp(at, 0, slots.Length);
            return caret;
        }

        // Caret after insertions: placedPos is the index after last placed slot and may equal slots.Length
        var caretAfterInsert = Math.Clamp(placedAt, 0, slots.Length);
        return caretAfterInsert;
    }

    bool? Autoset(TSymbol[] slots, int from, int to, bool allowOriginalValues = true)
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
                return null;
            }
            allowOriginalValues =
                allowOriginalValues && options.Contains(slots[i], _equalityComparer);
            if (allowOriginalValues is false)
            {
                slots[i] = options[0];
            }
        }

        return true;
    }

    TSymbol[] Replaced(TSymbol[] source, int from, int to, TSymbol with)
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

    void DefaultRight() { }

    record struct AutosetResult(TSymbol[] Slots, bool AllCharsAllowed, int LastIndex);
}
