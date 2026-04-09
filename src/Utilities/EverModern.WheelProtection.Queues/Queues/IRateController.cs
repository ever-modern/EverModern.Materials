using System;
using System.Threading;
using System.Threading.Tasks;

namespace EverModern.Threading.Queues;

/// <summary>
/// Provides rate limiting operations for asynchronous workflows.
/// </summary>
public interface IRateController
{
    /// <summary>
    /// Attempts to reserve a slot immediately.
    /// </summary>
    /// <param name="tryAgainAt">The earliest time a call can be retried if denied.</param>
    bool TryImmediately(out DateTimeOffset tryAgainAt);

    /// <summary>
    /// Awaits until a call is allowed.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask WhenAllowed(CancellationToken cancellationToken = default);
}
