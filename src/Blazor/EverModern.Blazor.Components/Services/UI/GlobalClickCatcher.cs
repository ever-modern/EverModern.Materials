using EverModern.Events;
using Microsoft.AspNetCore.Components.Web;

namespace EverModern.Blazor.Components.Services.UI;

public class GlobalClickCatcher : IGlobalClickCatcher, IGlobalClickInvoker
{
    List<Action<MouseEventArgs>> _mouseClickCallbacks = [];
    List<Action<KeyboardEventArgs>> _keyClickCallbacks = [];

    public Subscription OnMouseClicked(Action<MouseEventArgs, Subscription> action)
    {
        lock (this)
        {
            Subscription? sub = null;
            var argsAction = (MouseEventArgs args) => action(args, sub!);

            _mouseClickCallbacks.Add(argsAction);

            sub = new(_ => _mouseClickCallbacks.Remove(argsAction));

            return sub;
        }
    }

    public Subscription OnKeyPressed(Action<KeyboardEventArgs, Subscription> action)
    {
        lock (this)
        {
            Subscription? sub = null;
            var argsAction = (KeyboardEventArgs args) => action(args, sub!);

            _keyClickCallbacks.Add(argsAction);

            sub = new(_ => _keyClickCallbacks.Remove(argsAction));

            return sub;
        }
    }

    public void FireGlobalMouseClickEvent(MouseEventArgs args)
    {
        lock (this)
        {
            foreach (var callback in _mouseClickCallbacks)
            {
                try
                {
                    callback(args);
                }
                catch { }
            }
        }
    }

    public void FireKeyClickEvent(KeyboardEventArgs args)
    {
        lock (this)
        {
            GlobalLogger.Debug(
                $"Key {args.Key} is clicked. There are {_keyClickCallbacks.Count} callbacks to execute."
            );
            foreach (var callback in _keyClickCallbacks)
            {
                try
                {
                    callback(args);
                }
                catch (Exception ex)
                {
                    GlobalLogger.Error(
                        $"Key {args.Key} {callback} {callback.GetHashCode()} callback on target {callback.Target} failed executing: {ex}."
                    );
                }
            }
        }
    }
}
