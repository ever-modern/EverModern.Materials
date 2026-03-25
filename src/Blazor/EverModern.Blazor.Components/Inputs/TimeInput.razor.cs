using EverModern.WheelProtection.DataStructures.Text;
using Microsoft.AspNetCore.Components;

namespace EverModern.Blazor.Components.Inputs;

public partial class TimeInput
{
    private bool _showCustomPicker = false;

    [Parameter]
    public bool Clearable { get; set; }

    [Parameter]
    public string ValueNotSetText { get; set; } = "Not set";

    [Parameter]
    public TimeOnly MinValue { get; set; } = TimeOnly.MinValue;

    [Parameter]
    public TimeOnly MaxValue { get; set; } = TimeOnly.MaxValue;

    [Parameter]
    public bool IncludeSeconds { get; set; }

    bool _renderScheduled = false;

    void OnCharInputChanged(TimeMask newMask)
    {
        var newValue = newMask.Value;

        if (newValue != Value)
        {
            OnValueChanged(newValue);
            ScheduleRender();
        }
    }

    TimeOnly _initValue;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Value != _initValue)
        {
            _initValue = Value;
            ScheduleRender();
        }
    }

    private void OnInputFocus()
    {
        _showCustomPicker = true;
        ScheduleRender();
    }

    private void OnInputBlur()
    {
        // Delay hiding to allow clicking on picker items
        Task.Delay(200)
            .ContinueWith(_ =>
            {
                if ((_mouseOnInput || _mouseOnPicker) is false)
                {
                    _showCustomPicker = false;
                    InvokeAsync(ScheduleRender);
                }
            });
    }

    void ClearValue()
    {
        Value = default;
        var now = DateTime.Now;
        OnValueChanged(default);
        ScheduleRender();
    }

    private void SelectSecond(int day)
    {
        var newValue = new TimeOnly(Value.Hour, Value.Minute, day);
        OnValueChanged(newValue);
        ScheduleRender();
    }

    private void SelectMinute(int month)
    {
        var newValue = new TimeOnly(Value.Hour, month, Value.Second);
        OnValueChanged(newValue);
        ScheduleRender();
    }

    private void SelectHour(int hour)
    {
        var newValue = new TimeOnly(hour, Value.Minute, Value.Second);
        OnValueChanged(newValue);
        ScheduleRender();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender)
        {
            BindToLifetime(
                clickCatcher.OnMouseClicked(
                    (_, _) =>
                    {
                        if (TogglePicker(MouseIn))
                        {
                            ScheduleRender();
                        }
                    }
                )
            );
        }

        _renderScheduled = false;
    }

    bool TogglePicker(bool? show = null)
    {
        if (show == _showCustomPicker)
        {
            return false;
        }

        _showCustomPicker = !_showCustomPicker;

        return true;
    }

    bool MouseIn => _mouseOnInput || _mouseOnPicker;

    bool _mouseOnInput = false;
    bool _mouseOnPicker = false;

    static SelectOption<int>[] OptionsRange(int start, int finish, int minValue, int maxValue) =>
        [
            .. Enumerable
                .Range(start, finish - start + 1)
                .Select(number => new SelectOption<int>(
                    number,
                    number.ToString("D2"),
                    number > maxValue || number < minValue
                )),
        ];

    void ScheduleRender()
    {
        _renderScheduled = true;
        StateHasChanged();
    }

    protected override bool ShouldRender() => _renderScheduled;
}
