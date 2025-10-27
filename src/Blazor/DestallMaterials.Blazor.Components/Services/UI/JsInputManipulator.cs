using System.Threading;
using Microsoft.JSInterop;

namespace DestallMaterials.Blazor.Components.Services.UI;

public class JsInputManipulator : IInputManipulator
{
    readonly IJSRuntime _jSRuntime;

    const string _module = $"destallMaterials.uiManipulation.inputs";

    public JsInputManipulator(IJSRuntime jSRuntime)
    {
        _jSRuntime = jSRuntime;
    }

    public async Task<uint> GetCarretPositionAsync(
        string inputId,
        CancellationToken cancellationToken = default
    )
    {
        const string commandName = $"{_module}.getCarretPosition";
        var result = await _jSRuntime.InvokeAsync<uint?>(commandName, cancellationToken, [inputId]) ?? 0;
        return result;
    }

    public async Task SetCaretPositionAsync(
        string inputId,
        uint position,
        CancellationToken cancellationToken = default
    )
    {
        const string commandName = $"{_module}.setCaretPosition";
        await _jSRuntime.InvokeVoidAsync(commandName, cancellationToken, [inputId, position]);
    }

    public async Task SetSelectionRangeAsync(
        string inputId,
        uint start,
        uint end,
        CancellationToken cancellationToken = default
    )
    {
        const string commandName = $"{_module}.setSelectionRange";
        await _jSRuntime.InvokeVoidAsync(commandName, cancellationToken, [inputId, start, end]);
    }

    public async Task BlurAsync(string inputId, CancellationToken cancellationToken = default)
    {
        const string commandName = $"{_module}.blur";
        await _jSRuntime.InvokeVoidAsync(commandName, cancellationToken, [inputId]);
    }
}
