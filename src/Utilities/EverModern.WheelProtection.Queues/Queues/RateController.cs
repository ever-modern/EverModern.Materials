using EverModern.Chronos;
using EverModern.Threading.Locks;

namespace EverModern.Threading.Queues;

public sealed class RateController : IRateController
{
    readonly CallConstraint[] _constraints;
    readonly IChronos _clock;

    readonly Queue<DateTimeOffset>[] _calls;

    // Single lock because evaluation and commit must be atomic
    readonly Lock _lock = new();

    public RateController(
        IEnumerable<CallConstraint> constraints,
        IChronos clock)
    {
        _constraints = constraints.OptimizeConstraints().ToArray();
        _clock = clock;

        _calls = _constraints
            .Select(x => new Queue<DateTimeOffset>(x.MaxCallsCount))
            .ToArray();
    }

    public bool TryImmediately(out DateTimeOffset tryAgainAt)
    {
        var now = _clock.Now;

        using (_lock.LockScope())
        {
            return TryCommitCore(now, out tryAgainAt);
        }
    }

    public async ValueTask WhenAllowed(
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var now = _clock.Now;

            DateTimeOffset nextAllowedAt;

            using (_lock.LockScope())
            {
                if (TryCommitCore(now, out nextAllowedAt))
                    return;
            }

            var delay = nextAllowedAt - now;

            if (delay <= TimeSpan.Zero)
            {
                await Task.Yield();
                continue;
            }

            delay = ClampDelay(delay);

            await _clock.WhenComes(now + delay, cancellationToken);
        }
    }

    /// <summary>
    /// Must be called under _lock.
    /// Atomically evaluates and commits.
    /// </summary>
    bool TryCommitCore(
        DateTimeOffset now,
        out DateTimeOffset nextAllowedAt)
    {
        nextAllowedAt = DateTimeOffset.MaxValue;

        for (int i = 0; i < _constraints.Length; i++)
        {
            var constraint = _constraints[i];
            var queue = _calls[i];

            while (queue.Count > 0 &&
                   now - queue.Peek() > constraint.Period)
            {
                queue.Dequeue();
            }

            if (queue.Count < constraint.MaxCallsCount)
                continue;

            var candidate = queue.Peek() + constraint.Period;

            if (candidate < nextAllowedAt)
                nextAllowedAt = candidate;
        }

        if (nextAllowedAt != DateTimeOffset.MaxValue)
            return false;

        for (int i = 0; i < _calls.Length; i++)
        {
            _calls[i].Enqueue(now);
        }

        nextAllowedAt = now;

        return true;
    }

    static TimeSpan ClampDelay(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
            return TimeSpan.Zero;

        var max = TimeSpan.FromMilliseconds(int.MaxValue);

        return delay > max ? max : delay;
    }
}
