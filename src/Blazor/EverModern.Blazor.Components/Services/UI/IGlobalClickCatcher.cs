using Microsoft.AspNetCore.Components.Web;

namespace EverModern.Blazor.Components.Services.UI;

public class Subscription : IDisposable
{
    readonly Action<Subscription> _onCancelled;

    public Subscription(Action<Subscription> onDisposed)
    {
        _onCancelled = onDisposed;
    }

    public Subscription(Action onDisposed)
    {
        _onCancelled = th => onDisposed();
    }

    public void Cancel()
    {
        _onCancelled(this);
    }

    void IDisposable.Dispose()
        => Cancel();
}
public interface IGlobalClickCatcher
{
    Subscription OnKeyPressed(Action<KeyboardEventArgs, Subscription> action);
    Subscription OnMouseClicked(Action<MouseEventArgs, Subscription> action);
}

public interface IGlobalClickInvoker
{
    void FireGlobalMouseClickEvent(MouseEventArgs args);
    void FireKeyClickEvent(KeyboardEventArgs eventArgs);
}