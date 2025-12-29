using DestallMaterials.WheelProtection.DataStructures.Text;
using Microsoft.AspNetCore.Components;

namespace DestallMaterials.Blazor.Components.Inputs;

public partial class DateInput
{
    private bool _showCustomPicker = false;

    [Parameter]
    public bool Clearable { get; set; }

    [Parameter]
    public string ValueNotSetText { get; set; } = "Not set";

    [Parameter]
    public DateOnly MinValue { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(-50));

    [Parameter]
    public DateOnly MaxValue { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Parameter]
    public DateFormatting Format { get; set; } = DateFormatting.Default;

    bool _renderScheduled = false;

    void OnCharInputChanged(DateMask mask)
    {
        var newValue = mask.Value;
        if (newValue != Value)
        {
            OnValueChanged(newValue);
            ScheduleRender();
        }
    }
    
    DateOnly _initValue;

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

    private void SelectDay(int day)
    {
        var newValue = new DateOnly(Value.Year, Value.Month, day);
        OnValueChanged(newValue);
        ScheduleRender();
    }

    private void SelectMonth(int month)
    {
        var newValue = new DateOnly(Value.Year, month, Value.Day);
        OnValueChanged(newValue);
        ScheduleRender();
    }

    private void SelectYear(int year)
    {
        var newValue = new DateOnly(year, Value.Month, Value.Day);
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

    SelectOption<int>[] OptionsRange(int start, int finish, int minValue, int maxValue) =>
        [
            .. Enumerable
                .Range(start, finish - start + 1)
                .Select(month => new SelectOption<int>(
                    month,
                    month.ToString("D2"),
                    month > maxValue || month < minValue
                )),
        ];

    void ScheduleRender()
    {
        _renderScheduled = true;
        StateHasChanged();
    }

    protected override bool ShouldRender() => _renderScheduled;
}
