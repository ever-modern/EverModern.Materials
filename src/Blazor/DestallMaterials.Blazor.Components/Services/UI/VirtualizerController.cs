using Microsoft.AspNetCore.Components.Web.Virtualization;
using System.Reflection;

namespace DestallMaterials.Blazor.Services.UI;

public class VirtualizerController<TVirtualize>
{
    static readonly IReadOnlyList<FieldInfo> _virtualizerUnjustlyPrivateFields = new List<FieldInfo>
    {
        typeof(Virtualize<TVirtualize>).GetField("_itemsBefore", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException(),
        typeof(Virtualize<TVirtualize>).GetField("_itemCount", BindingFlags.NonPublic | BindingFlags.Instance ) ?? throw new InvalidOperationException(),
        typeof(Virtualize<TVirtualize>).GetField("_visibleItemCapacity", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException()
    };
    public static VirtualizerNumbers GetVirtualizerNumbers<T>(Virtualize<T> virtualize) => new VirtualizerNumbers(
            (int)_virtualizerUnjustlyPrivateFields[0].GetValue(virtualize),
            (int)_virtualizerUnjustlyPrivateFields[1].GetValue(virtualize),
            (int)_virtualizerUnjustlyPrivateFields[2].GetValue(virtualize)
        );

    readonly IUiManipulator _uiManipulator;
    readonly Virtualize<TVirtualize> _virtualize;
    readonly string _containerId;
    private readonly int _itemsShown;
    private readonly int _lineHeightPixels;

    public VirtualizerController(
            IUiManipulator uiManipulator,
            Virtualize<TVirtualize> virtualize,
            string containerId,
            int itemsShown,
            int lineHeightPixels
        )
    {
        _uiManipulator = uiManipulator;
        _virtualize = virtualize;
        _containerId = containerId;
        _itemsShown = itemsShown;
        _lineHeightPixels = lineHeightPixels;
    }

    public async Task ScrollToItem(int itemIndex)
    {
        var scrollTo = itemIndex * _virtualize.ItemSize;
        await _uiManipulator.ScrollItem_Y(_containerId, scrollTo);
    }
    public VirtualizerNumbers Numbers => GetVirtualizerNumbers(_virtualize);

    public async Task<TopBottomIndexPair> GetTopAndBottomIndexesAsync()
    {
        var currentScroll = await _uiManipulator.GetItemScroll_Y(_containerId);

        var topIndex = (int)Math.Round(currentScroll / _lineHeightPixels);

        var result = new TopBottomIndexPair
        {
            Top = topIndex,
            Bottom = topIndex + _itemsShown - 1
        };

        return result;
    }


    /// <summary>
    /// Scroll and set position of indexed line to certain position.
    /// </summary>
    /// <param name="lineNumber">Starts with 0.</param>
    /// <param name="scrollDestination"></param>
    /// <returns></returns>
    public async Task ScrollToLineAsync(int lineNumber, ScrollDestination scrollDestination)
    {
        var resultantScrollPosition = scrollDestination switch
        {
            ScrollDestination.Top => lineNumber,
            ScrollDestination.Bottom => lineNumber - _itemsShown + 1,
            ScrollDestination.Center => lineNumber - _itemsShown/2 + 1,
            _ => throw new NotImplementedException()
        } * _lineHeightPixels + 1;

        await _uiManipulator.ScrollItem_Y(_containerId, resultantScrollPosition);
    }
}

public enum ScrollDestination
{
    Top, Bottom, Center
}

public struct TopBottomIndexPair 
{
    public int Top { get; init; }
    public int Bottom { get; init; }
}
