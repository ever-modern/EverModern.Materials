using EverModern.Blazor.Components.Services.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EverModern.Blazor.Components.Common;

public abstract partial class ClickableComponent : ViewComponent
{
    [Inject]
    protected IGlobalClickCatcher GlobalClickCatcher { get; private set; }

    [Parameter]
    public bool Disabled { get; set; }

    protected virtual async Task OnInnerClickAsync(MouseEventArgs mouseEventArgs)
    {
    }

    protected virtual async Task OnOuterClickAsync(MouseEventArgs mouseEventArgs)
    { 
    }

    protected void OnGlobalClick(MouseEventArgs mouseEventArgs)
    {
        if (_mouseIn)
        {
            OnInnerClickAsync(mouseEventArgs).GetType();
        }
        else
        {
            OnOuterClickAsync(mouseEventArgs).GetType();
        }
    }

    protected bool _mouseIn;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    protected ClickableComponent()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        _onMouseIn = () => _mouseIn = true;
        _onMouseOut = () => _mouseIn = false;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender)
        {
            //Subscribe(GlobalClickCatcher.SubscribeForGlobalClick(OnGlobalClick));
        }
    }

    protected readonly Action _onMouseIn;
    protected readonly Action _onMouseOut;
}
