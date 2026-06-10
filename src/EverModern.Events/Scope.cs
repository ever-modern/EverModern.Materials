namespace EverModern.Events;

public class Scope : IDisposable
{
    readonly Lock _locker = new();

    readonly EventSource _beforeEnter = new();
    readonly EventSource _afterEnter = new();
    readonly EventSource _beforeExit = new();
    readonly EventSource _afterExit = new();

    bool _entered;
    bool _disposed;

    public INotifier BeforeEnter => _beforeEnter;
    public INotifier AfterEnter => _afterEnter;
    public INotifier BeforeExit => _beforeExit;
    public INotifier AfterExit => _afterExit;

    public static Scope EnterNew() => new Scope().Enter();

    public Scope Enter()
    {
        lock (_locker)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Scope));

            if (_entered)
                throw new InvalidOperationException("Scope already entered.");

            _beforeEnter.Invoke();
            _entered = true;
        }

        _afterEnter.Invoke();
        return this;
    }

    public void Dispose()
    {
        lock (_locker)
        {
            if (_disposed)
                return;

            if (_entered)
            {
                _beforeExit.Invoke();
                _entered = false;
                _afterExit.Invoke();
            }

            _disposed = true;

            ScopeExtensions.DisposeAll(_beforeEnter, _afterEnter, _beforeExit, _afterExit);
        }
    }
}
