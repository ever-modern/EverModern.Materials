using System.Runtime.CompilerServices;

namespace DestallMaterials.WheelProtection.DataStructures.Text;

public interface ISlotConstraintsSource<TSymbol, TConstraint>
{
    int Length { get; }

    TConstraint GetSlotConstraints(int slotIndex, IReadOnlyList<TSymbol> currentFilling);
}

public interface ISlotConstraintsSource<TSymbol>
    : ISlotConstraintsSource<TSymbol, SlotConstraint<TSymbol>> { }

public static class SlotConstraintsSourceExtensions
{
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
