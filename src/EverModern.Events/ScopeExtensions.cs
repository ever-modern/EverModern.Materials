namespace EverModern.Events;

/// <summary>
/// Provides helper methods for scope-related resource lifetime management.
/// </summary>
public static class ScopeExtensions
{
    /// <summary>
    /// Binds a disposable resource to a scope so it is disposed when the scope exits.
    /// </summary>
    /// <typeparam name="T">The disposable resource type.</typeparam>
    /// <param name="disposable">The resource to bind.</param>
    /// <param name="scope">The scope that controls the resource lifetime.</param>
    /// <returns>The same <paramref name="disposable"/> instance.</returns>
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

    /// <summary>
    /// Disposes all provided disposables in order.
    /// </summary>
    /// <param name="disposables">The disposables to dispose.</param>
    public static void DisposeAll(params ReadOnlySpan<IDisposable> disposables)
    {
        for (int i = 0; i < disposables.Length; i++)
        {
            disposables[i].Dispose();
        }
    }
}
