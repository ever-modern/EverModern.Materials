namespace EverModern.Events;

public static class ScopeExtensions
{
    public static T BoundToScope<T>(this T disposable, Scope scope)
        where T : IDisposable
    {
        scope.AfterExit.Subscribe(
            (sub) =>
            {
                disposable.Dispose();
                sub.Dispose();
            }
        );
        return disposable;
    }

    public static void DisposeAll(params ReadOnlySpan<IDisposable> disposables)
    {
        for (int i = 0; i < disposables.Length; i++)
        {
            disposables[i].Dispose();
        }
    }
}
