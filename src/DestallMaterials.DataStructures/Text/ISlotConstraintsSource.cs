namespace DestallMaterials.WheelProtection.DataStructures.Text;

public interface ISlotConstraintsSource<TSymbol>
{
    IReadOnlyList<SlotConstraint<TSymbol>> GetConstraints(IReadOnlyList<TSymbol> currentFilling);

    int Length { get; }
}