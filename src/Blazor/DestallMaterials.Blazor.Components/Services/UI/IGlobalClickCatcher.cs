using Microsoft.AspNetCore.Components.Web;

namespace DestallMaterials.Blazor.Components.Services.UI;

public class DisposableCallback : IDisposable
{
    readonly Action<DisposableCallback> _onDisposed;

    public DisposableCallback(Action<DisposableCallback> onDisposed)
    {
        _onDisposed = onDisposed;
    }

    public DisposableCallback(Action onDisposed)
    {
        _onDisposed = th => onDisposed();
    }

    public void Dispose()
    {
        _onDisposed(this);
    }
}
public interface IGlobalClickCatcher
{
    Task<MouseEventArgs> WhenMouseClicked(CancellationToken cancellationToken);
    Task<KeyboardEventArgs> WhenKeyPressed(CancellationToken cancellationToken);
}

public interface IGlobalClickInvoker
{
    void FireGlobalMouseClickEvent(MouseEventArgs args);
    void FireKeyClickEvent(KeyboardEventArgs eventArgs);
}