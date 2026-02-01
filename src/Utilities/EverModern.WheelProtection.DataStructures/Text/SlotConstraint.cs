namespace EverModern.WheelProtection.DataStructures.Text;

public readonly struct SlotConstraint<T>
{
    public IReadOnlyList<T> Options { get; }

    public SlotConstraint(IReadOnlyList<T> options)
    {
        Options = [.. options];
    }

    public static implicit operator SlotConstraint<T>(T[] options) => new(options);

    public static implicit operator SlotConstraint<T>(T singleOption) => new([singleOption]);

    public override string ToString() =>
        Options.Count == 0 ? "Empty"
        : Options.Count > 1 ? $"{Options[0]}-{Options[^1]}"
        : $"{Options[0]}";
}
