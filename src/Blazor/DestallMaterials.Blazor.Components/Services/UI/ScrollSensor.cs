using Microsoft.JSInterop;

namespace DestallMaterials.Blazor.Components.Services.UI;

public class ScrollStates
{
    public ScrollState Element { get; init; }
    public ScrollState Window { get; init; }
}

public struct ElementPosition
{
    public double X { get; init; }
    public double Y { get; init; }
}


public struct ScrollState
{
    public ElementPosition Position { get; init; }

    public double ScrolledHorizontally { get; init; }
    public double VisibleWidth { get; init; }


    public double VisibleHeight { get; init; }
    public double ScrolledVertically { get; init; }

    public double MaxVerticalScroll { get; init; }
    public double MaxHorizontalScroll { get; init; }
}

public interface IElementSensor<T>
{
    Task<DisposableCallback> SubscribeAsync(string elementId, Func<T, Task> callback);
}

public interface IScrollSensor : IElementSensor<ScrollState>
{
}

public class JsScrollSensor : IScrollSensor
{
    readonly Dictionary<string, List<Func<ScrollState, Task>>> _subscriptions = [];
    readonly IJSRuntime _js;

    const string _subscribeForScrollCommand = "destallMaterials.sensors.scroll.subscribe";

    public JsScrollSensor(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<DisposableCallback> SubscribeAsync(string id, Func<ScrollState, Task> callback)
    {
        byte result = 0;
        if (_subscriptions.TryGetValue(id, out var callbacks))
        {
            callbacks.Add(callback);
            result = 1;
        }
        else
        {
            _subscriptions[id] = new List<Func<ScrollState, Task>>()
            {
                callback
            };

            result = await _js.InvokeAsync<byte?>(_subscribeForScrollCommand, id, DotNetObjectReference.Create(this)) ?? 0;
        }

        if (result == 0)
        {
            return null;
        }

        return new DisposableCallback(() => _subscriptions[id].Remove(callback));
    }

    [JSInvokable]
    public async Task ReactToScrollEventAsync(
        string elementId,
        double[] elementScrollStateNumbers)
    {
        if (_subscriptions.TryGetValue(elementId, out var callbacks))
        {
            ScrollState elementState = elementScrollStateNumbers?.Any() is true ? CreateScrollStateFromInteropArray(elementScrollStateNumbers) : new();
            
            foreach (var callback in callbacks)
            {
                await callback(elementState);
            }
            return;
        }
    }

    static ScrollState CreateScrollStateFromInteropArray(double[] numbers)
        => CreateScrollState(numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5], numbers[6], numbers[7]);

    static ScrollState CreateScrollState(
        double height,
        double width,
        double scrolledVertically,
        double scrolledHorizontally,
        double maxVerticalScroll,
        double maxHorizontalScroll,
        double positionX,
        double positionY)
        => new()
        {
            ScrolledHorizontally = scrolledHorizontally,
            VisibleWidth = width,
            VisibleHeight = height,
            ScrolledVertically = scrolledVertically,
            MaxHorizontalScroll = maxHorizontalScroll,
            MaxVerticalScroll = maxVerticalScroll,
            Position = new ElementPosition
            {
                X = positionX,
                Y = positionY
            }
        };

}

public static class ScrollSensorExtensions
{
    public static Task<DisposableCallback> SubscribeForWindowScrollAsync(this IScrollSensor scrollSensor, Func<ScrollState, Task> callback)
        => scrollSensor.SubscribeAsync("__window", callback);
}
