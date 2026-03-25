namespace EverModern.WheelProtection.DataStructures.Text;



/// <summary>
/// Factory helpers for slot constraint sources.
/// </summary>
public static class SlotConstraintsSource
{
    /// <summary>
    /// Gets the default numeric symbols.
    /// </summary>
    public static IReadOnlyList<char> Numbers => [.. "0123456789"];

    /// <summary>
    /// Creates a slot constraint source with the specified length.
    /// </summary>
    /// <typeparam name="T">The symbol type.</typeparam>
    /// <param name="source">The constraint provider.</param>
    /// <param name="length">The slot count.</param>
    public static SlotConstraintsSource<T> Create<T>(
        Func<int, IReadOnlyList<T>, SlotConstraint<T>> source,
        int length
    ) => new(length, source);
}

/// <summary>
/// Provides slot constraints via a delegate.
/// </summary>
/// <typeparam name="T">The symbol type.</typeparam>
public class SlotConstraintsSource<T> : ISlotConstraintsSource<T>
{
    /// <inheritdoc />
    public int Length { get; }
    /// <summary>
    /// Gets the constraints delegate.
    /// </summary>
    public Func<int, IReadOnlyList<T>, SlotConstraint<T>> _getConstraints { get; }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="length">The slot count.</param>
    /// <param name="getConstraints">The constraint provider.</param>
    public SlotConstraintsSource(
        int length,
        Func<int, IReadOnlyList<T>, SlotConstraint<T>> getConstraints
    )
    {
        Length = length;
        _getConstraints = getConstraints;
    }

    /// <inheritdoc />
    public SlotConstraint<T> GetSlotConstraints(int slotIndex, IReadOnlyList<T> currentFilling)
        => _getConstraints(slotIndex, currentFilling);
}
