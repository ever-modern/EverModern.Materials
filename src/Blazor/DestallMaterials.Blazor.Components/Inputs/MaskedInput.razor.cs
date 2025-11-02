using DestallMaterials.Blazor.Components.Services.UI;
using DestallMaterials.WheelProtection.DataStructures.Text;
using Microsoft.AspNetCore.Components;

namespace DestallMaterials.Blazor.Components.Inputs;

public partial class MaskedInput
{
    public MaskedInput()
    {
        _inputId = $"masked-input-{this.GetHashCode()}";
        OnValueChanged = _ => { };
    }

    [Parameter]
    [EditorRequired]
    public Action<IReadOnlyList<char?>> OnValueChanged { get; set; }

    [Parameter]
    [EditorRequired]
    public ISlotConstraintsSource<char> ConstraintsSource { get; set; }

    [Parameter]
    [EditorRequired]
    public Func<IReadOnlyList<char?>, string> FormatMask { get; set; }

    [Parameter]
    [EditorRequired]
    public char?[] Value { get; set; }

    [Parameter]
    public IEqualityComparer<char?> EqualityComparer { get; set; } =
        EqualityComparer<char?>.Default;

    [Parameter]
    public string CssStyle { get; set; }

    [Parameter]
    public string CssClass { get; set; }

    public int CarretPosition => _lastPosition;

    Mask<char> _mask;

    protected override async Task OnParametersSetAsync()
    {
        base.OnParametersSet();
        _mask = new(ConstraintsSource, Value, EqualityComparer);        
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender)
        {
            BindToLifetime(
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
                )
            );
        }
    }

    readonly string _inputId;

    string DisplayText => FormatMask(_mask.Slots);

    int _lastPosition;

    async Task OnInput(string newValue)
    {
        var carretPosition = await GetCarretPositionAsync();

        var contentChange = ContentChange<char?>.Get(Value, [.. newValue.OfType<char?>()]);

        var selectedSlot = _mask.AcceptChange(contentChange);

        GlobalLogger.Debug($"Selected slot = {selectedSlot}");

        //StateHasChanged();

        OnValueChanged(_mask.Slots);

        await EnsureInputContentAsync();

        await MoveCarretAsync(selectedSlot - 1);
    }

    async Task OnClick()
    {
        var carretPosition = await GetCarretPositionAsync();
        _lastPosition = carretPosition;
        //if (carretPosition < Constraints.Length)
        //{
        //    await SelectCharsAsync(carretPosition, carretPosition + 1);
        //}
    }

    IInputManipulator Js => ui.Inputs;

    async Task<int> GetCarretPositionAsync()
    {
        var result = (int)await Js.GetCarretPositionAsync(_inputId);
        GlobalLogger.Debug($"Carret position is {result}");
        return result;
    }

    async Task MoveCarretAsync(int newPosition) =>
        await Js.SetCaretPositionAsync(_inputId, (uint)newPosition);

    async Task SelectCharsAsync(int start, int end) =>
        await Js.SetSelectionRangeAsync(_inputId, (uint)start, (uint)end);

    async Task EnsureInputContentAsync() => await Js.SetInputValueAsync(_inputId, DisplayText);
}
