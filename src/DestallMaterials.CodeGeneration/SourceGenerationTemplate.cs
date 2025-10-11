using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace DestallMaterials.CodeGeneration;

public abstract class SourceGenerationTemplate : ComponentBase
{
    public static class SourceCodeRenderingSymbols
    {
        public static readonly MarkupString GreaterSign = (MarkupString)">";
        public static readonly MarkupString Arrow = (MarkupString)"=>";
        public static readonly MarkupString G = GreaterSign;
        public static readonly MarkupString L = (MarkupString)"<";
        public static MarkupString Raw(string str) => (MarkupString)str;
        public static MarkupString A => Arrow;
    }

    [Parameter]
    [EditorRequired]
    public ICompilationSource CompilationSource { get; set; }
}

public sealed class Content : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public RenderFragment ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);
        ChildContent(builder);
    }
}

public sealed class SourceFile : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        builder.OpenElement(1, "File");
        builder.AddAttribute(2, nameof(Virtual), Virtual ? "true" : "false");
        builder.AddAttribute(3, nameof(Path), Path.ToString());
        builder.AddContent(4, ChildContent);
        builder.CloseElement();
    }

    [Parameter]
    [EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool Virtual { get; set; }

    [Parameter]
    [EditorRequired]
    public ProjectRelativeFilePath Path { get; set; }
}