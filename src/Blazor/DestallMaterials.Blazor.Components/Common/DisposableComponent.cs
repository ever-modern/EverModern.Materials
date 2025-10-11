using DestallMaterials.WheelProtection.DataStructures;
using Microsoft.AspNetCore.Components;

namespace DestallMaterials.Blazor.Components.Universal;


public class DisposableComponent : ComponentBase, IDisposable
{
    DisposableList<IDisposable> BoundItems = new DisposableList<IDisposable>();
    CancellationTokenSource _cancellationTokenSource = new();

    protected DisposableComponent()
    {
        UntilDisposed = _cancellationTokenSource.Token;
    }

    public void Dispose()
    {
        BeforeDispose();
        BoundItems.Dispose();
        _cancellationTokenSource.Cancel();
    }

    protected virtual void BeforeDispose()
    {
        
    }


    protected void BindToLifetime(IDisposable disposable) => BoundItems.Add(disposable);

    public CancellationToken UntilDisposed { get; }
}
