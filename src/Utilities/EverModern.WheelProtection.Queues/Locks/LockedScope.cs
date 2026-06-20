using EverModern.Events;

namespace EverModern.Threading.Locks;

/// <summary>
/// A disposable scope that wraps a <see cref="Lock"/> or <see cref="SemaphoreSlim"/>
/// and fires lifecycle events (<see cref="Scope.BeforeEnter"/>, <see cref="Scope.BeforeExit"/>, etc.)
/// on enter and exit. The underlying lock is acquired on enter and released on exit.
/// </summary>
public class LockedScope : Scope
{
    readonly Lock? _lock;
    readonly SemaphoreSlim? _semaphore;

    int _entered;

    internal LockedScope()
    {
        // nothing except lifecycle binding
    }

    LockedScope(Lock @lock) : this()
    {
        _lock = @lock;

        BeforeEnter.Subscribe(_ => EnterLock());
        BeforeExit.Subscribe(_ => ExitLock());
    }

    LockedScope(SemaphoreSlim semaphore) : this()
    {
        _semaphore = semaphore;

        BeforeExit.Subscribe(_ => ExitSemaphore());
    }

    /// <summary>
    /// Creates and enters a <see cref="LockedScope"/> for the specified <see cref="Lock"/>.
    /// The lock is acquired immediately and released on scope exit.
    /// </summary>
    /// <param name="lock">The lock to enter.</param>
    /// <returns>An entered scope that owns the lock.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="lock"/> is null.</exception>
    public static LockedScope Enter(Lock @lock)
    {
        ArgumentNullException.ThrowIfNull(@lock);

        var scope = new LockedScope(@lock);
        scope.Enter();

        return scope;
    }

    /// <summary>
    /// Creates and enters a <see cref="LockedScope"/> for the specified <see cref="SemaphoreSlim"/>.
    /// The semaphore is awaited asynchronously and released on scope exit.
    /// </summary>
    /// <param name="semaphore">The semaphore to acquire.</param>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    /// <returns>An entered scope that owns the semaphore.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="semaphore"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown if cancellation is requested.</exception>
    public static async ValueTask<LockedScope> EnterAsync(
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(semaphore);

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        var scope = new LockedScope(semaphore);
        scope.Enter();

        return scope;
    }

    void EnterLock()
    {
        if (Interlocked.Exchange(ref _entered, 1) != 0)
            throw new InvalidOperationException("Scope already entered.");

        _lock!.Enter();
    }

    /// <summary>
    /// Enters this scope, making <see cref="Scope.Exit"/> fire lifecycle events.
    /// Used when the scope is created un-entered (e.g. via the parameterless constructor)
    /// and needs to be entered before use.
    /// </summary>
    internal void EnterScope() => Enter();

    void ExitLock() { _lock!.Exit(); }

    void ExitSemaphore() { _semaphore!.Release(); }
}
