using Microsoft.AspNetCore.Components;

namespace EverModern.Blazor.Components.Common;

public interface IOuterRefreshable
{
    void Rerender();
}

public interface IExposedReference<T>
    where T : IComponent, IExposedReference<T>
{
    Action<T> ReferenceAction { set; }
}