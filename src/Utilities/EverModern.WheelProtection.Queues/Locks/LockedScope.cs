using EverModern.Events;

namespace EverModern.Threading.Locks;

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

    public static LockedScope Enter(Lock @lock)
    {
        ArgumentNullException.ThrowIfNull(@lock);

        var scope = new LockedScope(@lock);
        scope.Enter();

        return scope;
    }

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

    void ExitLock() { _lock!.Exit(); }

    void ExitSemaphore() { _semaphore!.Release(); }
}
