namespace EverModern.Threading.Queues;

public static class LockedScopeExtensions
{
    public static ValueTask<AsyncScopeLocker> LockScopeAsync(
        this SemaphoreSlim locker,
        CancellationToken cancellationToken = default
    ) => AsyncScopeLocker.EnterAsync(locker, cancellationToken);

    public static ScopeLocker LockScope(this Lock locker) => new(locker);
}
