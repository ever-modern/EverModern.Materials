using EverModern.WheelProtection.DataStructures.Events;
using Microsoft.AspNetCore.Components.Web;

namespace EverModern.Blazor.Components.Services.UI;
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