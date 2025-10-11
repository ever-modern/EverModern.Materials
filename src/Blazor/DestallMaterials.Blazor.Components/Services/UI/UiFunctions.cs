using Microsoft.AspNetCore.Components.Web.Virtualization;
using System.Reflection;

namespace DestallMaterials.Blazor.Services.UI;

public static class UiFunctions
{

}

public struct VirtualizerNumbers
{
    public int ItemsBefore { get; init; }
    public int VisibleItemCapacity { get; init; }
    public int ItemsCount { get; init; }

    public VirtualizerNumbers(int itemsBefore, int visibleItemCapacity, int itemsCount)
    {
        ItemsBefore = itemsBefore;
        VisibleItemCapacity = visibleItemCapacity;
        ItemsCount = itemsCount;
    }
}
