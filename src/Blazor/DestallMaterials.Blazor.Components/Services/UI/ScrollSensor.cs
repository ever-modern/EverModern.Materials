using Microsoft.JSInterop;
using System.Collections;

namespace DestallMaterials.Blazor.Components.Services.UI;

public record ScrollStates(ScrollState Element, ScrollState Window);

public readonly record struct ElementPosition(
    double X,
    double Y);

public readonly record struct ScrollState(
    ElementPosition Position,
    double ScrolledHorizontally,
    double VisibleWidth,
    double VisibleHeight,
    double ScrolledVertically,
    double MaxVerticalScroll,
    double MaxHorizontalScroll);

public interface IElementSensor<T>
{
    Task<Subscription> SubscribeAsync(string elementId, Func<T, Task> callback);
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

    public async Task<Subscription> SubscribeAsync(string id, Func<ScrollState, Task> callback)
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

        return new Subscription(() => _subscriptions[id].Remove(callback));
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
        => CreateScrollState(
            height: numbers[0],
            width: numbers[1],
            scrolledVertically: numbers[2],
            scrolledHorizontally: numbers[3],
            maxVerticalScroll: numbers[4],
            maxHorizontalScroll: numbers[5],
            positionX: numbers[6],
            positionY: numbers[7]);

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
            Position = new ElementPosition(positionX, positionY)
        };

}

public static class ScrollSensorExtensions
{
    public static Task<Subscription> SubscribeForWindowScrollAsync(this IScrollSensor scrollSensor, Func<ScrollState, Task> callback)
        => scrollSensor.SubscribeAsync("__window", callback);
}

