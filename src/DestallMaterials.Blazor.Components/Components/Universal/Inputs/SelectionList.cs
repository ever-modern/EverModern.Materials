using DestallMaterials.Blazor.Functions;
using DestallMaterials.Blazor.Services.Extensions;
using DestallMaterials.Blazor.Services.UI;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components;

namespace DestallMaterials.Blazor.Components.Universal.Inputs;

public partial class SelectionList<TItem> : ClickableComponent
{
    [Parameter]
    public int MaxItemsShown { get; set; } = 10;

    [Parameter]
    public Action<TItem> OnItemClicked { get; set; } = i => { };

    [Parameter]
    public Action<TItem> OnItemExcluded { get; set; } = i => { };

    [Parameter]
    [EditorRequired]
    public Task<int> ItemsCountTotal { get; set; }

    [Parameter]
    [EditorRequired]
    public Func<int, CancellationToken, Task<IList<TItem>>> GetBatch { get; set; }

    [Parameter]
    public int BatchSize { get; set; } = 10;

    [Parameter]
    public Func<TItem, string> GetItemRepresentation { get; set; } = i => i.ToString();

    [Parameter]
    public Func<TItem, TItem, bool> ItemsComparison { get; set; } = (i1, i2) => i1.Equals(i2);

    [Parameter]
    public string InputLabel { get; set; }

    [Parameter]
    public Func<TItem, bool> ItemIsEmpty { get; set; } = i => i == null;

    [Parameter]
    public Action OnListClick { get; set; } = null;

    [Parameter]
    public bool IsActive { get; set; }

    [Parameter]
    public Action<TItem> OnArrowSelect { get; set; } = item => { };

    [Parameter]
    public Action OnFocus { get; set; } = () => { };

    public async Task RerenderAsync()
    {
        if (Virtualize == null)
        {
            return;
        }
        SelectedItemIndex = 0;
        await Virtualize.RefreshDataAsync();
    }

    private void _onFocus()
    {
        if (OnFocus != null)
        {
            OnFocus();
        }
    }

    Virtualize<TItem> Virtualize;

    readonly string _elementId = Guid.NewGuid().ToString();

    async Task<IEnumerable<TItem>> DownloadItemsOnDemandAsync(int startIndex, int count, int pageSize, CancellationToken cancellationToken)
    {
        return await DynamicLoading.LoadForVirtualizationInPagesAsync<TItem>(
            startIndex,
            count,
            (uint)pageSize,
            async (p,ct) => await GetBatch((int)p, ct),
            cancellationToken
        );
    }

    TItem SelectedItem;

    async Task<ItemsProviderResult<TItem>> ProvideItems(ItemsProviderRequest request)
    {
        var items = await DownloadItemsOnDemandAsync(request.StartIndex, request.Count, BatchSize, request.CancellationToken);
        if (!items.Any())
        {
            SelectedItemIndex = -1;
        }

        var listHeight = _listLineHeight * Math.Min(MaxItemsShown, await ItemsCountTotal);
        ListStyle = $"height: {listHeight}px; line-height: {_listLineHeight}px";

        return new(items, await ItemsCountTotal);
    }

    public int SelectedItemIndex { get; private set; } = -1;

    async Task ClickItem(TItem item)
    {
        OnItemClicked(item);
    }

    void SpacePressed()
    {
        if (SelectedItemIndex != -1)
        {
            var selectedItem = SelectedItem;
            if (!ItemIsEmpty(selectedItem))
            {
                OnItemClicked(SelectedItem);
            }
        }
    }

    void OnMouseIn()
    {
        _mouseIn = true;
    }
    void OnMouserOut()
    {
        _mouseIn = false;
    }

    async Task ArrowClicked(ArrowDirection arrowDirection)
    {
        if (!IsActive || VirtualizerController == null)
        {
            return;
        }
        var topBottom = await VirtualizerController.GetTopAndBottomIndexesAsync();

        SelectedItemBoundaries = topBottom;

        if (SelectedItemIndex <= topBottom.Bottom && SelectedItemIndex >= topBottom.Top)
        {
            return;
        }

        var scrollDestination = arrowDirection switch
        {
            ArrowDirection.Up => ScrollDestination.Top,
            ArrowDirection.Down => ScrollDestination.Bottom,
            _ => throw new NotImplementedException()
        };

        await VirtualizerController.ScrollToLineAsync(SelectedItemIndex, scrollDestination);
    }

    async Task ArrowUp()
    {
        if (SelectedItemIndex > 0)
        {
            SelectedItemIndex--;
            await ArrowClicked(ArrowDirection.Up);
            StateHasChanged();
        }
    }

    async Task ArrowDown()
    {
        if (SelectedItemIndex < await ItemsCountTotal - 1)
        {
            SelectedItemIndex++;
            await ArrowClicked(ArrowDirection.Down);
            StateHasChanged();
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Subscribe(globalClickCatcher.OnKeyPressed(Key.ArrowUp, async e => await ArrowUp()));
            Subscribe(globalClickCatcher.OnKeyPressed(Key.ArrowDown, async e => await ArrowDown()));
            Subscribe(globalClickCatcher.OnKeyPressed(Key.Space, e => SpacePressed()));
            Subscribe(globalClickCatcher.OnKeyPressed(Key.Enter, e => SpacePressed()));
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
    }

    protected override void OnParametersSet()
    {
        if (TopBottomIndexes.Top == 0 && TopBottomIndexes.Bottom == 0)
        {
            TopBottomIndexes = new TopBottomIndexPair
            {
                Top = 0,
                Bottom = (int)MaxItemsShown
            };
        }

        var listHeight = _listLineHeight * MaxItemsShown;
        ListStyle = $"height: {listHeight}px; line-height: {_listLineHeight}px";
    }

    VirtualizerController<TItem> VirtualizerController;

    public TopBottomIndexPair TopBottomIndexes { get; private set; }

    string ListStyle;

    ElementReference _listReference;

    IList<TItem> Items = new List<TItem>();

    const int _listLineHeight = 20;
}

public enum ArrowDirection
{
    Up, Down
}
