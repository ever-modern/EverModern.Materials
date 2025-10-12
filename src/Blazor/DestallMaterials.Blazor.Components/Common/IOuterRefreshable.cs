using Microsoft.AspNetCore.Components;

namespace DestallMaterials.Blazor.Components.Common;

public interface IOuterRefreshable
{
    void Rerender();
}

public interface IExposedReference<T>
    where T : IComponent, IExposedReference<T>
{
    Action<T> ReferenceAction { set; }
}