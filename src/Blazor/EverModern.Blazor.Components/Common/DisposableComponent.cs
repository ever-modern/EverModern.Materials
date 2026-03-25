using EverModern.Blazor.Components.Services.UI;
using Microsoft.AspNetCore.Components;

namespace EverModern.Blazor.Components.Common;

public class DisposableComponent : ComponentBase, IDisposable
{
    readonly List<IDisposable> BoundItems = [];
    readonly CancellationTokenSource _cancellationTokenSource = new();

    protected bool Disposed { get; private set; }

    protected DisposableComponent()
    {
        UntilDisposed = _cancellationTokenSource.Token;
    }

    public void Dispose()
    {
        BeforeDispose();
        Disposed = true;

        GlobalLogger.Debug($"{GetType()} {GetHashCode()} component is being disposed.");

        foreach (var item in BoundItems)
        {
            try
            {
                item.Dispose();
            }
            catch (Exception ex)
            {
                GlobalLogger.Error(
                    $"Error disposing to-lifetime-bound item {item.GetType()} {item.GetHashCode()}: {ex}"
                );
            }
        }

        _cancellationTokenSource.Cancel();

        GlobalLogger.Debug($"{GetType()} {GetHashCode()} component has been disposed.");
    }

    protected virtual void BeforeDispose() { }

    protected void BindToLifetime(params ReadOnlySpan<IDisposable> disposable)
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
        BoundItems.AddRange(disposable);
    }

    public CancellationToken UntilDisposed { get; }
}
