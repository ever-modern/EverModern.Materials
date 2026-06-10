using EverModern.Events;

namespace EverModern.Threading;

public static class LockedScopeExtensions
{
    public static ValueTask<LockedScope> LockScopeAsync(
        this SemaphoreSlim locker,
        CancellationToken cancellationToken = default
    ) => LockedScope.EnterAsync(locker, cancellationToken);

    public static LockedScope LockScope(this Lock locker) => LockedScope.Enter(locker);
}
