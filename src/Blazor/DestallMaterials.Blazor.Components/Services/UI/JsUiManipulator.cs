using Microsoft.JSInterop;

namespace DestallMaterials.Blazor.Services.UI;


public class JsUiManipulator : IUiManipulator
{
    private readonly IJSRuntime _runtime;

    public JsUiManipulator(IJSRuntime runtime)
    {
        this._runtime = runtime;
    }

    public async Task ScrollItem_X(string id, double deltaX)
    {
        const string command = "scrollElement_X";
        await _runtime.InvokeVoidAsync(command, id, deltaX);
    }

    public async Task ScrollItem_Y(string id, double deltaY)
    {
        const string command = "scrollElement_Y";
        await _runtime.InvokeVoidAsync(command, id, deltaY);
    }

    public async Task<uint> Y_Deviation(string itemId, string containerId)
    {
        const string deviation = "Y_elementDeviation";
        var result = await _runtime.InvokeAsync<uint>(deviation, itemId, containerId);
        return result;
    }

    public async Task<uint> X_Deviation(string itemId, string containerId)
    {
        const string deviation = "X_elementDeviation";
        var result = await _runtime.InvokeAsync<uint>(deviation, itemId, containerId);
        return result;
    }

    public async Task ScrollToFit_Y(string itemId, string containerId)
    {
        const string command = "Y_scrollToFit";
        await _runtime.InvokeVoidAsync(command, itemId, containerId);
    }

    public async Task SetCssVariableValue(string elementId, string variableName, string value)
    {
        const string command = $"setCssVariableValue";
        await _runtime.InvokeVoidAsync(command, elementId, variableName, value);
    }

    public async Task<double> GetItemScroll_Y(string elementId)
    {
        const string command = "getItemScroll_Y";
        var result = await _runtime.InvokeAsync<double>(command, elementId);
        return result;
    }

    public async Task DisableDefaultEventHandling(string elementId, string eventType)
    {
        const string command = "disableDefaultHandling";
        await _runtime.InvokeVoidAsync(command, elementId, eventType);
    }

    public async Task<ElementBoungingRectangle?> GetElementBoungingRectangle(string elementId)
    {
        const string command = "destallMaterials.uiManipulation.getBoundingRectangle";
        var numbers = await _runtime.InvokeAsync<double[]>(command, elementId);

        if (numbers is null)
        {
            return default;
        }

        var result = new ElementBoungingRectangle
        {
            Top = numbers[0],
            Bottom = numbers[1],
            Left = numbers[2],
            Right = numbers[3],
            Width = numbers[4],
            Height = numbers[5]
        };

        return result;
    }
}
