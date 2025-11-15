namespace DestallMaterials.WheelProtection.DataStructures.Text;

public interface ISlotConstraintsSource<TSymbol, TConstraint>
{
    int Length { get; }

    TConstraint GetSlotConstraints(int slotIndex, IReadOnlyList<TSymbol> currentFilling);
}

public interface ISlotConstraintsSource<TSymbol>
    : ISlotConstraintsSource<TSymbol, SlotConstraint<TSymbol>> { }
