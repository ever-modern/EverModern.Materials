using System.ComponentModel;

namespace EverModern.WheelProtection.DataStructures;

/// <summary>
/// Represents a void-like value.
/// </summary>
public readonly struct Nothing
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static Nothing Instance { get; } = default;

    /// <summary>
    /// Initializes a new instance. Use <see cref="Instance"/> instead.
    /// </summary>
    [Obsolete("Don't create new instances. Use the static 'Instance' property.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Nothing()
    {
    }
}

/// <summary>
/// Represents a typed void-like value.
/// </summary>
/// <typeparam name="T">The associated type.</typeparam>
public readonly struct Nothing<T>
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static Nothing<T> Instance { get; } = default;

    /// <summary>
    /// Initializes a new instance. Use <see cref="Instance"/> instead.
    /// </summary>
    [Obsolete("Don't create new instances. Use the static 'Instance' property.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Nothing()
    {
    }
}