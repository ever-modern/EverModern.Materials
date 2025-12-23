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
    public string Format { get; set; } = "dd.MM.yyyy";

    bool _renderScheduled = false;

    void OnCharInputChanged(IReadOnlyList<char> chars)
    {
        var charsArray = chars.ToArray();
        _valueChars = charsArray;

        var (day, month, year) = DateFormatting.BreakIntoComponents(charsArray);

        var newValue = Value;

        if (int.TryParse(year, out var yearNumber) && yearNumber != Value.Year)
        {
            var selectedYear = yearNumber;
            try
            {
                newValue = new DateOnly(yearNumber, newValue.Month, newValue.Day);
            }
            catch { }
        }

        if (int.TryParse(month, out var monthNumber) && monthNumber != Value.Month)
        {
            var selectedMonth = monthNumber;
            try
            {
                newValue = new DateOnly(newValue.Year, monthNumber, newValue.Day);
            }
            catch { }
        }

        if (int.TryParse(day, out var dayNumber) && dayNumber != Value.Day)
        {
            var selectedDay = dayNumber;
            try
            {
                newValue = new DateOnly(newValue.Year, newValue.Month, dayNumber);
            }
            catch { }
        }

        if (newValue != Value)
        {
            OnValueChanged(newValue);
            ScheduleRender();
        }
    }

    DateFormatting DateFormatting => DateFormatting.Parse(Format);
    char[] _valueChars = [];

    DateOnly _initValue;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Value != _initValue)
        {
            _initValue = Value;
            _valueChars = [.. _initValue.ToString(Format)];
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
