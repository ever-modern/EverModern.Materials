namespace EverModern.WheelProtection.Executions;

/// <summary>
/// Provides a human-readable explanation for an exception or result.
/// </summary>
public interface IExplained
{
    /// <summary>
    /// Gets the explanation text.
    /// </summary>
    string Explanation { get; }
}
