using Microsoft.AspNetCore.Components;

namespace DestallMaterials.Blazor.Components
{
    public abstract class StandardItemComponent<TModel>
    {
        public abstract TModel Model { get; set; }
        public abstract RenderFragment Render();
    }

    public abstract class ViewTableStandardComponent<TModel, TFilter> : StandardItemComponent<IList<TModel>>
    {
        public abstract TFilter Filter { get; set; }
    }

}
