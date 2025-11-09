using DestallMaterials.WheelProtection.DataStructures.Time;

namespace DestallMaterials.WheelProtection.DataStructures.Text;

public static class SlotConstraintsSource
{
    public static IReadOnlyList<char> Numbers => [.. "0123456789"];

    public static SlotConstraintsSource<T> Create<T>(
        Func<IReadOnlyList<T>, IReadOnlyList<SlotConstraint<T>>> source,
        int length
    ) => new(length, source);
}

public class SlotConstraintsSource<T> : ISlotConstraintsSource<T>
{
    public int Length { get; }
    public Func<IReadOnlyList<T>, IReadOnlyList<SlotConstraint<T>>> _getConstraints { get; }

    public SlotConstraintsSource(
        int length,
        Func<IReadOnlyList<T>, IReadOnlyList<SlotConstraint<T>>> getConstraints
    )
    {
        Length = length;
        _getConstraints = getConstraints;
    }

    public IReadOnlyList<SlotConstraint<T>> GetConstraints(IReadOnlyList<T> currentFilling) =>
        _getConstraints(currentFilling);
}

