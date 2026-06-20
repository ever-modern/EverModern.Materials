using EverModern.Chronos;

namespace EverModern.Threading.Queues;

/// <summary>
/// Completion-based rate limiter that releases capacity when operations finish.
/// </summary>
public class CompletionRateController : ICompletionRateController
{
    readonly IChronos _clock;
    readonly CallConstraint[] _constraints;

    readonly PriorityQueue<Slot, DateTimeOffset>[] _queues;
    readonly Lock _lock = new();

    sealed class Slot
    {
        public DateTimeOffset ReleaseAt;
    }

    public CompletionRateController(
        IEnumerable<CallConstraint> constraints,
        IChronos clock)
    {
        _constraints = constraints.OptimizeConstraints().ToArray();
        _clock = clock;

        _queues = new PriorityQueue<Slot, DateTimeOffset>[_constraints.Length];

        for (int i = 0; i < _queues.Length; i++)
            _queues[i] = new PriorityQueue<Slot, DateTimeOffset>();
    }

    public CompletionRateController(IEnumerable<CallConstraint> constraints)
        : this(constraints, RealtimeChronos.Instance) {}

    public bool TryTakeImmediately(out ContinuationToken token)
    {
        lock (_lock)
        {
            var now = _clock.Now;

            if (!TryAcquire(now, out _))
            {
                token = null!;
                return false;
            }

            token = CreateToken();
            return true;
        }
    }

    public async ValueTask<ContinuationToken> WhenAllowed(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DateTimeOffset nextAllowedAt;

            lock (_lock)
            {
                var now = _clock.Now;

                if (TryAcquire(now, out nextAllowedAt))
                    return CreateToken();
            }

            await _clock.WhenComes(nextAllowedAt, cancellationToken);
        }
    }

    bool TryAcquire(DateTimeOffset now, out DateTimeOffset nextAllowedAt)
    {
        nextAllowedAt = DateTimeOffset.MaxValue;

        for (int i = 0; i < _constraints.Length; i++)
        {
            var constraint = _constraints[i];
            var pq = _queues[i];

            while (pq.Count > 0 && pq.Peek().ReleaseAt <= now)
                pq.Dequeue();

            if (pq.Count < constraint.MaxCallsCount)
                continue;

            var earliest = pq.Peek().ReleaseAt;
            if (earliest < nextAllowedAt)
                nextAllowedAt = earliest;
        }

        return nextAllowedAt == DateTimeOffset.MaxValue;
    }

    ContinuationToken CreateToken()
    {
        var released = 0;
        var ownedSlots = new Slot[_constraints.Length];

        return new RateControlLocker(() =>
            {
                if (Interlocked.Exchange(ref released, 1) != 0)
                    return;

                var now = _clock.Now;

                lock (_lock)
                {
                    for (int i = 0; i < _constraints.Length; i++)
                    {
                        var slot = new Slot
                        {
                            ReleaseAt = now + _constraints[i].Period
                        };

                        ownedSlots[i] = slot;
                        _queues[i].Enqueue(slot, slot.ReleaseAt);
                    }
                }
            }
        );
    }
}
