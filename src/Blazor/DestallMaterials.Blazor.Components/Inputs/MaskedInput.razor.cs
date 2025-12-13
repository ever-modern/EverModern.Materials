using DestallMaterials.Blazor.Components.Services.UI;
using DestallMaterials.WheelProtection.DataStructures.Text;
using Microsoft.AspNetCore.Components;

namespace DestallMaterials.Blazor.Components.Inputs;

public partial class MaskedInput<TMask> : BaseInput<TMask>
    where TMask : IImmutableMask<char, TMask>
{
    public MaskedInput()
    {
        _inputId = $"masked-input-{this.GetHashCode()}";
        base.OnValueChanged = _ => { };
    }

    [Parameter]
    public string? Placeholder { get; set; }

    public int CarretPosition => _lastPosition;

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender)
        {
            BindToLifetime(
                Js.OnChange(
                    _inputId,
                    (currentState) =>
                    {
                        var (newValue, carretPosition) = currentState;

                        var mask = Value;

                        var oldValue = ToString(Value);

                        var contentChange = ContentChange<char>.Get(
                            oldValue,
                            newValue,
                            carretPosition
                        );

                        if (contentChange.At >= mask.Count) { }

                        GlobalLogger.Debug(
                            $"Inferred change from {oldValue} -> {newValue} with finishing carret position {carretPosition}: {contentChange}"
                        );

                        mask = mask.Change(contentChange, out carretPosition);

                        GlobalLogger.Debug($"Carret position = {carretPosition}");

                        //StateHasChanged();

                        base.OnValueChanged(mask);

                        var displayText = ToString(mask);

                        return new(displayText, carretPosition);
                    }
                )
            );
        }
    }

    readonly string _inputId;
    int _lastPosition;

    IInputManipulator Js => ui.Inputs;

    static string ToString(TMask? mask) => mask is null ? "" : new string([.. mask]);
}
