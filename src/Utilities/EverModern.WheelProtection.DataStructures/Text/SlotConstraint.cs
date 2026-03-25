namespace EverModern.WheelProtection.DataStructures.Text;

/// <summary>
/// Defines available options for a mask slot.
/// </summary>
/// <typeparam name="T">The option type.</typeparam>
public readonly struct SlotConstraint<T>
{
    /// <summary>
    /// Gets the available options.
    /// </summary>
    public IReadOnlyList<T> Options { get; }

    /// <summary>
    /// Initializes a new constraint with options.
    /// </summary>
    /// <param name="options">The available options.</param>
    public SlotConstraint(IReadOnlyList<T> options)
    {
        Options = [.. options];
    }

    /// <summary>
    /// Converts an options array to a slot constraint.
    /// </summary>
    /// <param name="options">The available options.</param>
    public static implicit operator SlotConstraint<T>(T[] options) => new(options);

    /// <summary>
    /// Converts a single option to a slot constraint.
    /// </summary>
    /// <param name="singleOption">The option.</param>
    public static implicit operator SlotConstraint<T>(T singleOption) => new([singleOption]);

    /// <inheritdoc />
    public override string ToString() =>
        Options.Count == 0 ? "Empty"
        : Options.Count > 1 ? $"{Options[0]}-{Options[^1]}"
        : $"{Options[0]}";
}
