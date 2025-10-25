using DestallMaterials.WheelProtection.DataStructures;
using Microsoft.AspNetCore.Components;

namespace DestallMaterials.Blazor.Components.Common;

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

        foreach (var item in BoundItems)
        {
            try
            {
                item.Dispose();
            }
            catch { }
        }

        _cancellationTokenSource.Cancel();
    }

    protected virtual void BeforeDispose() { }

    protected void BindToLifetime(params ReadOnlySpan<IDisposable> disposable)
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
        BoundItems.AddRange(disposable);
    }

    public CancellationToken UntilDisposed { get; }
}
