namespace EverModern.Events;

public interface IObservableScope
{
    INotifier BeforeEnter { get; }
    INotifier AfterEnter { get; }
    INotifier BeforeExit { get; }
    INotifier AfterExit { get; }
}

public class Scope : IObservableScope, IDisposable
{
    readonly object _locker = new();

    readonly EventSource _beforeEnter = new();
    readonly EventSource _afterEnter = new();
    readonly EventSource _beforeExit = new();
    readonly EventSource _afterExit = new();

    bool _entered;
    bool _disposed;

    public INotifier BeforeEnter => ThrowIfDisposed(_beforeEnter);
    public INotifier AfterEnter => ThrowIfDisposed(_afterEnter);
    public INotifier BeforeExit => ThrowIfDisposed(_beforeExit);
    public INotifier AfterExit => ThrowIfDisposed(_afterExit);

    public static Scope EnterNew()
    {
        var scope = new Scope();
        scope.Enter();
        return scope;
    }

    protected Scope Enter()
    {
        lock (_locker)
        {
            ThrowIfDisposedLocked();

            if (_entered)
                throw new InvalidOperationException("Scope already entered.");

            SafeInvoke(_beforeEnter);
            _entered = true;
        }

        SafeInvoke(_afterEnter);
        return this;
    }

    public void Exit()
    {
        EventSource beforeExit, afterExit;

        lock (_locker)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (!_entered)
            {
                DisposeSources();
                return;
            }

            beforeExit = _beforeExit;
            afterExit = _afterExit;
        }

        // Outside lock: callbacks must not block state transitions
        SafeInvoke(beforeExit);

        lock (_locker)
        {
            _entered = false;
        }

        SafeInvoke(afterExit);

        DisposeSources();
    }

    void DisposeSources()
    {
        ScopeExtensions.DisposeAll(
            _beforeEnter,
            _afterEnter,
            _beforeExit,
            _afterExit
        );
    }

    static void SafeInvoke(EventSource source)
    {
        try
        {
            source.Invoke();
        }
        catch
        {
            // swallow or optionally log
            // important: lifecycle must not break due to observers
        }
    }

    void ThrowIfDisposedLocked()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Scope));
    }

    T ThrowIfDisposed<T>(T value)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Scope));

        return value;
    }
    
    void IDisposable.Dispose()
        => Exit();
}
