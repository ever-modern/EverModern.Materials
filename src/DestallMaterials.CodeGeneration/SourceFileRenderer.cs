using BlazorTemplater;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using System.Web;

namespace DestallMaterials.CodeGeneration;

/// <summary>
/// Serves to render source code components with Compilation.
/// </summary>
public sealed class SourceFileRenderer
{
    readonly IServiceProvider _serviceProvider;
    readonly ICompilationSource _compilationSource;

    /// <summary>
    /// Create new instance of <see cref="SourceFileRenderer" />
    /// </summary>
    /// <param name="configureServices">COnfigure dependency injection container. Service scope is defined by <see cref="SourceGenerationTemplate" /> rendering.</param>
    public SourceFileRenderer(ICompilationSource compilationSource, Action<IServiceCollection> configureServices)
    {
        var services = new ServiceList();
        configureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        _compilationSource = compilationSource;
    }

    /// <summary>
    /// Render source code template to Source code files.
    /// </summary>
    /// <typeparam name="TComponent"></typeparam>
    /// <returns></returns>
    public IEnumerable<CodeFile> RenderSourceCode<TComponent>(IEnumerable<KeyValuePair<string, object>> parameters)
        where TComponent : SourceGenerationTemplate
    {
        var renderer = new ComponentRenderer<DynamicComponent>();

        using var scope = _serviceProvider.CreateScope();

        renderer.AddServiceProvider(_serviceProvider);

        var parametersToPass = parameters.Append(new(nameof(SourceGenerationTemplate.CompilationSource), _compilationSource));

        renderer.Set(c => c.Type, typeof(TComponent));
        renderer.Set(c => c.Parameters, parametersToPass.ToDictionary());

        var stringResult = renderer.Render();

        stringResult = HttpUtility.HtmlDecode(stringResult);

        var codeFiles = CodeFile.ParseMany(stringResult);

        return codeFiles;
    }
}