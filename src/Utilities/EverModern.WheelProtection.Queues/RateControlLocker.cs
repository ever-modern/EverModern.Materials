using System;

namespace EverModern.WheelProtection.Queues;

/// <summary>
/// Continuation token that releases a rate slot on disposal.
/// </summary>
public class RateControlLocker : ContinuationToken, IDisposable
{
    readonly Action _onDisposed;

    /// <summary>
    /// Initializes a new instance of the locker.
    /// </summary>
    /// <param name="onDisposed">The callback to invoke when released.</param>
    public RateControlLocker(Action onDisposed)
    {
        _onDisposed = onDisposed;
    }

    /// <inheritdoc />
    public override void Dispose() => _onDisposed();
}
