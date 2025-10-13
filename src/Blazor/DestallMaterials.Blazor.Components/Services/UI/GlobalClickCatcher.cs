using Microsoft.AspNetCore.Components.Web;

namespace DestallMaterials.Blazor.Components.Services.UI;

public class GlobalClickCatcher : IGlobalClickCatcher, IGlobalClickInvoker
{
    TaskCompletionSource<MouseEventArgs> _mouseClickTcs = new();
    TaskCompletionSource<KeyboardEventArgs> _keyClickTcs = new();

    public Task<KeyboardEventArgs> WhenKeyPressed(CancellationToken cancellationToken)
    {
        lock (this)
        {
            return _keyClickTcs.Task;
        }
    }

    public Task<MouseEventArgs> WhenMouseClicked(CancellationToken cancellationToken)
    {
        lock (this)
        {
            return _mouseClickTcs.Task;
        }
    }

    public void FireGlobalMouseClickEvent(MouseEventArgs args)
    {
        lock (this)
        {
            _mouseClickTcs.TrySetResult(args);
            _mouseClickTcs = new();
        }
    }

    public void FireKeyClickEvent(KeyboardEventArgs eventArgs)
    {
        lock (this)
        {
            _keyClickTcs.TrySetResult(eventArgs);
            _keyClickTcs = new();
        }
    }
}
