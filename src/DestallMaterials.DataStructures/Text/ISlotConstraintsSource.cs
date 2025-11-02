namespace DestallMaterials.WheelProtection.DataStructures.Text;

public interface ISlotConstraintsSource<TSymbol>
    where TSymbol : struct
{
    IReadOnlyList<SlotConstraint<TSymbol>> GetConstraints(IReadOnlyList<TSymbol?> currentFilling);

    int Length { get; }
}