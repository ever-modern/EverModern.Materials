namespace DestallMaterials.WheelProtection.DataStructures.Text;

public interface ISlotConstraintsSourceOld<TSymbol>
{
    IReadOnlyList<SlotConstraint<TSymbol>> GetConstraints(IReadOnlyList<TSymbol> currentFilling);

    int Length { get; }
}