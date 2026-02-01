namespace EverModern.Blazor.Components.Containers;

sealed class ModelRenderPiecePair<TModel>
{
    public ModelRenderPiecePair(TModel model)
    {
        this.Model = model;
    }

    public TModel Model { get; set; }
    public RenderPiece RenderPiece { get; set; }
}
