using Microsoft.AspNetCore.Components;

namespace DestallMaterials.Blazor.Components.Universal;

public interface IOuterRefreshable
{
    void Rerender();
}

public interface IExposedReference<T>
    where T : IComponent, IExposedReference<T>
{
    Action<T> ReferenceAction { set; }
}