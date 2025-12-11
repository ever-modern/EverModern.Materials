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
    public Func<IReadOnlyList<char>, string> FormatMask { get; set; } = (x) => new string([.. x]);

    [Parameter]
    public IEqualityComparer<char> EqualityComparer { get; set; } = EqualityComparer<char>.Default;

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

                        SimpleMask<char> mask = new(
                            Value ?? ConstraintsSource.GetDefaultValue(),
                            ConstraintsSource,
                            EqualityComparer
                        );

                        var oldValue = FormatMask(base.Value ?? []);

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

                        var displayText = FormatMask(mask);

                        return new(displayText, carretPosition);
                    }
                )
            );
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Value = Value ?? ConstraintsSource.GetDefaultValue();
    }

    readonly string _inputId;
    int _lastPosition;

    IInputManipulator Js => ui.Inputs;
}
