using DestallMaterials.Blazor.Components.Common;
using DestallMaterials.Blazor.Components.Services.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace DestallMaterials.Blazor.Components.Containers;

public enum RenderTriggerBehaviourType
{
    Automatic = 0,
    StrictlyManual = 1,
}

public abstract class Area : DisposableComponent
{
    [Inject]
    protected IGlobalClickCatcher globalClick { get; set; } = null!;

    [Inject]
    protected IUiManipulator uiManipulator { get; set; } = null!;

    [Parameter]
    public Action<MouseEventArgs>? OnOuterClick { get; set; }

    [Parameter]
    public Action<MouseEventArgs>? OnInnerClick { get; set; }

    [Parameter]
    public Action<KeyboardEventArgs>? OnKeyClicked { get; set; }

    [Parameter]
    public Action<MouseEventArgs>? OnMouseEnter { get; set; }

    [Parameter]
    public Action<MouseEventArgs>? OnMouseLeave { get; set; }

    [Parameter]
    public Action<FocusEventArgs>? OnBlur { get; set; }

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public string CssClass { get; set; } = "";

    [Parameter]
    public string CssStyle { get; set; } = "";

    [Parameter]
    public string HoveredCssClass { get; set; } = "";

    [Parameter]
    public RenderTriggerBehaviourType RenderTriggerBehaviour { get; set; } =
        RenderTriggerBehaviourType.Automatic;

    public void Refresh()
    {
        _shouldRerender = true;
        StateHasChanged();
    }

    public bool MouseIn
    {
        get;
        protected set
        {
            if (field != value)
            {
                field = value;
                if (string.IsNullOrEmpty(HoveredCssClass) == false)
                {
                    _shouldRerender = true;
                    StateHasChanged();
                }
            }
        }
    } = false;

    bool _shouldRerender = false;

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender)
        {
            BindToLifetime(
                globalClick.OnMouseClicked(
                    (keyClick, _) =>
                    {
                        if (MouseIn is false)
                        {
                            OnOuterClick?.Invoke(keyClick);
                        }
                    }
                )
            );
        }

        _shouldRerender = false;
    }

    protected override bool ShouldRender() =>
        RenderTriggerBehaviour switch
        {
            RenderTriggerBehaviourType.Automatic => base.ShouldRender(),
            RenderTriggerBehaviourType.StrictlyManual => _shouldRerender,
            _ => throw new NotSupportedException(
                $"RenderTriggerBehaviourType '{RenderTriggerBehaviour}' is not supported."
            ),
        };

    protected string Class =>
        MouseIn && !string.IsNullOrEmpty(HoveredCssClass)
            ? $"{CssClass} {HoveredCssClass}"
            : CssClass;

    RenderTreeBuilder AddEventCallback<TArgs>(
        RenderTreeBuilder renderTreeBuilder,
        int index,
        string attributeName,
        Action<TArgs>? callback,
        out int sequence
    )
    {
        sequence = index;
        if (callback is null)
            return renderTreeBuilder;

        renderTreeBuilder.AddAttribute(
            sequence,
            attributeName,
            new EventCallback<TArgs>(this, callback)
        );

        sequence++;
        return renderTreeBuilder;
    }

    protected virtual void SetAttributes(RenderTreeBuilder renderTreeBuilder)
    {
        int sequence = 1;

        renderTreeBuilder.AddAttribute(++sequence, "class", Class);
        renderTreeBuilder.AddAttribute(++sequence, "style", CssStyle);
        renderTreeBuilder.AddAttribute(++sequence, "id", Id);

        AddEventCallback(renderTreeBuilder, sequence, "onclick", OnInnerClick, out sequence);
        AddEventCallback(renderTreeBuilder, sequence, "onblur", OnBlur, out sequence);
        AddEventCallback(renderTreeBuilder, sequence, "onkeydown", OnKeyClicked, out sequence);
        AddEventCallback(renderTreeBuilder, sequence, "onclick", OnInnerClick, out sequence);

        AddEventCallback(
            renderTreeBuilder,
            sequence,
            "onmouseenter",
            (MouseEventArgs e) =>
            {
                if (OnMouseEnter is not null)
                {
                    OnMouseEnter(e);
                }

                MouseIn = true;
            },
            out sequence
        );

        AddEventCallback(
            renderTreeBuilder,
            sequence,
            "onmouseleave",
            (MouseEventArgs e) =>
            {
                if (OnMouseLeave is not null)
                {
                    OnMouseLeave(e);
                }

                MouseIn = false;
            },
            out sequence
        );
    }

    protected RenderFragment RenderElementWithAttributes(string elementName) =>
        renderTreeBuilder =>
        {
            renderTreeBuilder.OpenElement(0, elementName);
            SetAttributes(renderTreeBuilder);
            renderTreeBuilder.AddContent(1, ChildContent);
            renderTreeBuilder.CloseElement();
        };

    [Parameter]
    public string Id { get; set; }

    public Area()
    {
        Id = $"{Guid.NewGuid()}";
    }
}
