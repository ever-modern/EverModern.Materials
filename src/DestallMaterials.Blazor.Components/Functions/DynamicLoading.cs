using DestallMaterials.WheelProtection.Extensions.Enumerables;

namespace DestallMaterials.Blazor.Functions;

public static class DynamicLoading
{
    /// <summary>
    /// Algorythm to load exact items range,based on page loading function (pages numbering starts from 1).
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="startIndex"></param>
    /// <param name="countRequested"></param>
    /// <param name="pageSize"></param>
    /// <param name="pageLoadingFunction">Function that calculates pages content from page number, which starts from 1.</param>
    /// <returns></returns>
    public static async Task<List<TItem>> LoadForVirtualizationInPagesAsync<TItem>(
        int startIndex,
        int countRequested,
        uint pageSize,
        Func<uint, CancellationToken, Task<IEnumerable<TItem>>> pageLoadingFunction,
        CancellationToken cancellationToken)
    {
        var pagesNeeded = (int)Math.Ceiling((decimal)(countRequested + startIndex) / pageSize);
        var result = new List<TItem>();
        uint startPageNumber = (uint)(startIndex / pageSize + 0.5) + 1;
        var offset = startIndex % pageSize;

        var loadingTasks = new Task<IEnumerable<TItem>>[pagesNeeded];
        for (uint p = 0; p < pagesNeeded; p++)
        {
            var pageNumberForTask = p;
            var itemsTask = pageLoadingFunction(startPageNumber + pageNumberForTask, cancellationToken);
            loadingTasks[p] = itemsTask;
        }
        for (uint p = 0; p < pagesNeeded; p++)
        {
            var itemsTask = loadingTasks[p];
            var items = (await itemsTask).EnsureMaterialized();

            if (p == 0)
            {
                result.AddRange(items.Skip((int)offset));
            }
            else if (p == pagesNeeded - 1)
            {
                result.AddRange(items.Take(countRequested - result.Count));
            }
            else
            {
                result.AddRange(items);
            }
            if (items.Count < pageSize)
            {
                break;
            }
        }

        return result;
    }
}
