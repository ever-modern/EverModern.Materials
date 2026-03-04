namespace EverModern.WheelProtection.Caching;

/// <summary>
/// Defines caching parameters for cached values.
/// </summary>
public struct CachingSettings
{
    /// <summary>
    /// Gets the duration for which cached values remain valid.
    /// </summary>
    public TimeSpan Validity { get; init; }
    /// <summary>
    /// Gets the maximum cache size.
    /// </summary>
    public readonly int MaxSize { get; init; }
}