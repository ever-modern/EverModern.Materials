namespace DestallMaterials.WheelProtection.DataStructures.Text;

public record struct SlotConstraint<T>(IReadOnlyList<T?> Options)
    where T : struct;