using System.Text.Json;
using Microsoft.JSInterop;

namespace EverModern.Blazor.Components.Services.UI;

public class JsInputManipulator : IInputManipulator
{
    readonly IJSRuntime _jSRuntime;

    const string _module = $"destallMaterials.uiManipulation.inputs";

    static List<object> _dontFinalize = [];

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
        var result =
            await _jSRuntime.InvokeAsync<uint?>(commandName, cancellationToken, [inputId]) ?? 0;
        return result;
    }

    public Task SetCaretPositionAsync(
        string inputId,
        uint position,
        CancellationToken cancellationToken = default
    ) =>
        SetSelectionRangeAsync(
            inputId: inputId,
            start: position,
            end: position,
            cancellationToken: cancellationToken
        );

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

    public async Task SetInputValueAsync(
        string inputId,
        string value,
        CancellationToken cancellationToken = default
    )
    {
        const string commandName = $"{_module}.setValue";
        await _jSRuntime.InvokeVoidAsync(commandName, cancellationToken, [inputId, value]);
    }

    public async Task<Subscription> OnChange(
        string inputId,
        Func<TextInputState, TextInputState> processState
    )
    {
        const string commandName = $"{_module}.subscribeForChange";
        JsCallbackWrapper callback = new(state0 =>
        {
            var state = new TextInputState(
                NewText: ((JsonElement)state0[0]).Deserialize<string>() ?? "",
                CarretFinishedAt: ((JsonElement)state0[1]).Deserialize<int>()
            );
            var newState = processState(state);
            return [newState.NewText, newState.CarretFinishedAt];
        });

        var dotRef = DotNetObjectReference.Create(callback);
        // keep the DotNetObjectReference alive until unsubscribed
        _dontFinalize.Add(dotRef);

        var result =
            await _jSRuntime.InvokeAsync<object>(
                commandName,
                CancellationToken.None,
                [inputId, dotRef]
            )
            ?? throw new InvalidOperationException(
                $"Could not subscribe for input {inputId} change."
            );

        GlobalLogger.Debug("Subsribed for change.");

        return new Subscription(() =>
        {
            try
            {
                _dontFinalize.Remove(dotRef);
                dotRef.Dispose();
            }
            catch { }
        });
    }

    public record struct TextInputState(string NewText, int CarretFinishedAt);
}

public class JsCallbackWrapper
{
    readonly Func<object[], object[]> _callback;

    public JsCallbackWrapper(Func<object[], object[]> callback)
    {
        _callback = callback;
    }

    [JSInvokable]
    public object[] Invoke(object[] args) => _callback(args);
}
