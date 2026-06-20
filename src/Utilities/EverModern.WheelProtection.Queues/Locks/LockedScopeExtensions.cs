namespace EverModern.Threading.Locks;

/// <summary>
/// Provides convenience methods for entering <see cref="LockedScope"/> instances.
/// </summary>
public static class LockedScopeExtensions
{
    /// <summary>
    /// Asynchronously acquires a scope for the specified semaphore.
    /// </summary>
    /// <param name="locker">The semaphore to wait on.</param>
    /// <param name="cancellationToken">A token used to cancel waiting for the semaphore.</param>
    /// <returns>A value task containing the acquired locked scope.</returns>
    public static ValueTask<LockedScope> LockScopeAsync(
        this SemaphoreSlim locker,
        CancellationToken cancellationToken = default
    ) => LockedScope.EnterAsync(locker, cancellationToken);

    /// <summary>
    /// Acquires a scope for the specified lock.
    /// </summary>
    /// <param name="locker">The lock to enter.</param>
    /// <returns>The acquired locked scope.</returns>
    public static LockedScope LockScope(this Lock locker) => LockedScope.Enter(locker);
}
