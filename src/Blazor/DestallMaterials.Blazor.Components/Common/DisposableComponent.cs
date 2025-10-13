using DestallMaterials.WheelProtection.DataStructures;
using Microsoft.AspNetCore.Components;

namespace DestallMaterials.Blazor.Components.Common;


public class DisposableComponent : ComponentBase, IDisposable
{
    DisposableList<IDisposable> BoundItems = new DisposableList<IDisposable>();
    CancellationTokenSource _cancellationTokenSource = new();
    protected bool Disposed { get; private set; }

    protected DisposableComponent()
    {
        UntilDisposed = _cancellationTokenSource.Token;
    }

    public void Dispose()
    {
        BeforeDispose();
        Disposed = true;
        BoundItems.Dispose();
        _cancellationTokenSource.Cancel();
    }

    protected virtual void BeforeDispose()
    {

    }

    protected void BindToLifetime(IDisposable disposable) => BoundItems.Add(disposable);

    public CancellationToken UntilDisposed { get; }

    protected void ForLifetime(Func<CancellationToken, Task> action)
    {
        Task.Run(async () => 
        {
            while (Disposed is false)
            {
                await action(UntilDisposed);
            }
        });
    }
}
