using Microsoft.AspNetCore.Components.Web;

namespace DestallMaterials.Blazor.Components.Services.UI;

public class GlobalClickCatcher : IGlobalClickCatcher, IGlobalClickInvoker
{
    readonly List<Action<KeyboardEventArgs>> _keyClickCallbacks = new();
    readonly List<Action<MouseEventArgs>> _mouseClickCallbacks = new();

    readonly List<Func<KeyboardEventArgs, Task>> _keyClickCallbacksAsync = new();
    readonly List<Func<MouseEventArgs, Task>> _mouseClickCallbacksAsync = new();

    public async Task FireGlobalMouseClickEvent(MouseEventArgs eventArgs)
    {
        foreach (var callback in _mouseClickCallbacks)
        {
            callback(eventArgs);
        }
        await Task.WhenAll(_mouseClickCallbacksAsync.Select(c => c(eventArgs)).ToArray());
    }

    public async Task FireKeyClickEvent(KeyboardEventArgs eventArgs)
    {
        foreach (var callback in _keyClickCallbacks)
        {
            callback(eventArgs);
        }
        await Task.WhenAll(_keyClickCallbacksAsync.Select(c => c(eventArgs)).ToArray());
    }

    public DisposableCallback SubscribeForGlobalClick(Action<MouseEventArgs> onMouseClick)
    {
        var dc = new DisposableCallback(e => _mouseClickCallbacks.Remove(onMouseClick));
        _mouseClickCallbacks.Add(onMouseClick);
        return dc;
    }

    public DisposableCallback SubscribeForKeyClick(Action<KeyboardEventArgs> onKeyClick)
    {
        var dc = new DisposableCallback(e => _keyClickCallbacks.Remove(onKeyClick));
        _keyClickCallbacks.Add(onKeyClick);
        return dc;
    }

    public DisposableCallback SubscribeForKeyClick(Func<KeyboardEventArgs, Task> onKeyClick)
    {
        var dc = new DisposableCallback(e => _keyClickCallbacksAsync.Remove(onKeyClick));
        _keyClickCallbacksAsync.Add(onKeyClick);
        return dc;
    }

    public DisposableCallback SubscribeForGlobalClick(Func<MouseEventArgs, Task> onMouseClick)
    {
        var dc = new DisposableCallback(e => _mouseClickCallbacksAsync.Remove(onMouseClick));
        _mouseClickCallbacksAsync.Add(onMouseClick);
        return dc;
    }
}
