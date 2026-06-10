using EverModern.Events;

namespace EverModern.Threading;

public class LockedScope : Scope
{
    private readonly Lock? _lock;
    private readonly SemaphoreSlim? _semaphore;

    private int _entered;

    private LockedScope(Lock @lock)
    {
        _lock = @lock;
        BeforeEnter.Subscribe(_ => OnEnterLock());
        AfterExit.Subscribe(_ => OnExitLock());
    }

    private LockedScope(SemaphoreSlim semaphore)
    {
        _semaphore = semaphore;
        BeforeEnter.Subscribe(_ => OnEnterSemaphore());
        AfterExit.Subscribe(_ => OnExitSemaphore());
    }

    public static LockedScope Enter(Lock @lock)
    {
        ArgumentNullException.ThrowIfNull(@lock);

        var scope = new LockedScope(@lock);
        scope.Enter();

        return scope;
    }

    public static async ValueTask<LockedScope> EnterAsync(SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(semaphore);

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        var scope = new LockedScope(semaphore);
        scope.Enter();

        return scope;
    }

    private void OnEnterLock()
    {
        if (Interlocked.Exchange(ref _entered, 1) != 0)
            throw new InvalidOperationException();

        _lock!.Enter();
    }

    private void OnExitLock()
    {
        _lock!.Exit();
    }

    private void OnEnterSemaphore()
    {
        if (Interlocked.Exchange(ref _entered, 1) != 0)
            throw new InvalidOperationException();
    }

    private void OnExitSemaphore()
    {
        _semaphore!.Release();
    }
}
