namespace DestallMaterials.WheelProtection.DataStructures.Text;

public struct SlotConstraint<T>
    where T : struct
{
    public IReadOnlyList<T?> Options { get; }

    public SlotConstraint(IReadOnlyList<T?> options)
    {
        Options = [.. options];
    }

    public static implicit operator SlotConstraint<T>(T?[] options) => new(options);

    public static implicit operator SlotConstraint<T>(T singleOption) => new([singleOption]);
}
