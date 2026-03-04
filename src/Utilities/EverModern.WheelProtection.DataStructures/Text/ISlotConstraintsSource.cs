namespace EverModern.WheelProtection.DataStructures.Text;

/// <summary>
/// Provides constraints for mask slots.
/// </summary>
/// <typeparam name="TSymbol">The symbol type.</typeparam>
/// <typeparam name="TConstraint">The constraint type.</typeparam>
public interface ISlotConstraintsSource<TSymbol, TConstraint>
{
    /// <summary>
    /// Gets the slot count.
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Gets constraints for a slot.
    /// </summary>
    /// <param name="slotIndex">The slot index.</param>
    /// <param name="currentFilling">The current content.</param>
    TConstraint GetSlotConstraints(int slotIndex, IReadOnlyList<TSymbol> currentFilling);
}

/// <summary>
/// Provides slot constraints using <see cref="SlotConstraint{T}"/>.
/// </summary>
/// <typeparam name="TSymbol">The symbol type.</typeparam>
public interface ISlotConstraintsSource<TSymbol>
    : ISlotConstraintsSource<TSymbol, SlotConstraint<TSymbol>> { }

/// <summary>
/// Extensions for slot constraint sources.
/// </summary>
public static class SlotConstraintsSourceExtensions
{
    /// <summary>
    /// Builds the default value using the first option in each slot.
    /// </summary>
    /// <typeparam name="T">The symbol type.</typeparam>
    /// <param name="slotConstraintsSource">The slot constraint source.</param>
    public static T[] GetDefaultValue<T>(this ISlotConstraintsSource<T> slotConstraintsSource)
    {
        var length = slotConstraintsSource.Length;
        var result = new T[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = slotConstraintsSource.GetSlotConstraints(i, result).Options[0];
        }

        return result;
    }
}
