namespace EverModern.Chronos;


/// <summary>
/// Chronos implementation that uses real system time.
/// </summary>
public sealed class RealtimeChronos : IChronos
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static RealtimeChronos Instance { get; } = new RealtimeChronos();

    RealtimeChronos()
    {
    }

    /// <inheritdoc />
    public DateTimeOffset Now => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public Task WhenComes(DateTimeOffset targetTime, CancellationToken cancellationToken = default)
    {
        var delay = targetTime - Now;

        // handle past time safely
        if (delay <= TimeSpan.Zero)
            return Task.CompletedTask;

        return Task.Delay(delay, cancellationToken);
    }

    /// <inheritdoc />
    public Task WhenComes(DateTime targetTimeUtc, CancellationToken cancellationToken = default)
        => WhenComes(new DateTimeOffset(targetTimeUtc, TimeSpan.Zero), cancellationToken);

    /// <inheritdoc />
    public Task WhenPasses(TimeSpan time, CancellationToken cancellationToken = default)
    {
        if (time <= TimeSpan.Zero)
            return Task.CompletedTask;

        return Task.Delay(time, cancellationToken);
    }
}
