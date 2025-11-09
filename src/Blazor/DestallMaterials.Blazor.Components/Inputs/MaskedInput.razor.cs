using DestallMaterials.Blazor.Components.Services.UI;
using DestallMaterials.WheelProtection.DataStructures.Text;
using Microsoft.AspNetCore.Components;

namespace DestallMaterials.Blazor.Components.Inputs;

public partial class MaskedInput : BaseInput<IReadOnlyList<char>>
{
    public MaskedInput()
    {
        _inputId = $"masked-input-{this.GetHashCode()}";
        base.OnValueChanged = _ => { };
    }

    [Parameter]
    [EditorRequired]
    public ISlotConstraintsSource<char> ConstraintsSource { get; set; }

    [Parameter]
    [EditorRequired]
    public Func<IReadOnlyList<char>, string> FormatMask { get; set; }

    [Parameter]
    public IEqualityComparer<char> EqualityComparer { get; set; } =
        EqualityComparer<char>.Default;

    [Parameter]
    public string? Placeholder { get; set; }

    public int CarretPosition => _lastPosition;

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender)
        {
            base.BindToLifetime(
                globalClickCatcher.OnKeyPressed(
                    async (_, _) =>
                    {
                        _lastPosition = await GetCarretPositionAsync();
                    }
                ),
                globalClickCatcher.OnMouseClicked(
                    async (_, _) =>
                    {
                        _lastPosition = await GetCarretPositionAsync();
                    }
                ),
                Js.OnChange(_inputId, (currentState) => 
                {
                  var (newValue, carretPosition) = currentState;

                    Mask<char> mask = _mask;

                    var oldValue = FormatMask(base.Value ?? []);

                    var contentChange = ContentChange<char>.Get(
                        oldValue,
                        newValue,
                        carretPosition
                    );

                    GlobalLogger.Debug(
                        $"Inferred change from {oldValue} -> {newValue} with finishing carret position {carretPosition}: {contentChange}"
                    );

                    carretPosition = mask.AcceptChange(contentChange);

                    GlobalLogger.Debug($"Carret position = {carretPosition}");

                    //StateHasChanged();

                    base.OnValueChanged(mask.Slots);

                    var displayText = FormatMask(mask.Slots);

                    return new(displayText, carretPosition);
                })
            );
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        _mask = new(ConstraintsSource, base.Value ?? [], EqualityComparer);
    }

    readonly string _inputId;
    Mask<char> _mask;
    int _lastPosition;

    IInputManipulator Js => ui.Inputs;

    async Task<int> GetCarretPositionAsync()
    {
        var result = (int)await Js.GetCarretPositionAsync(_inputId);
        GlobalLogger.Debug($"Carret position is {result}");
        return result;
    }
}
