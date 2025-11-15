using DestallMaterials.WheelProtection.DataStructures.Time;

namespace DestallMaterials.WheelProtection.DataStructures.Text;



public static class SlotConstraintsSource
{
    public static IReadOnlyList<char> Numbers => [.. "0123456789"];

    public static SlotConstraintsSource<T> Create<T>(
        Func<int, IReadOnlyList<T>, SlotConstraint<T>> source,
        int length
    ) => new(length, source);
}

public class SlotConstraintsSource<T> : ISlotConstraintsSource<T>
{
    public int Length { get; }
    public Func<int, IReadOnlyList<T>, SlotConstraint<T>> _getConstraints { get; }

    public SlotConstraintsSource(
        int length,
        Func<int, IReadOnlyList<T>, SlotConstraint<T>> getConstraints
    )
    {
        Length = length;
        _getConstraints = getConstraints;
    }

    public SlotConstraint<T> GetSlotConstraints(int slotIndex, IReadOnlyList<T> currentFilling)
        => _getConstraints(slotIndex, currentFilling);
}
