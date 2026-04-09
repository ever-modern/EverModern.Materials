using System;
using System.Threading;
using System.Threading.Tasks;

namespace EverModern.Threading.Queues;

/// <summary>
/// Represents a disposable continuation token for completion-based rate limiting.
/// </summary>
public abstract class ContinuationToken : IDisposable
{
    /// <inheritdoc />
    public abstract void Dispose();
}

/// <summary>
/// Provides completion-based rate limiting operations.
/// </summary>
public interface ICompletionRateController
{
    /// <summary>
    /// Awaits a token that must be disposed when the operation completes.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask<ContinuationToken> WhenAllowed(CancellationToken cancellationToken);
}
