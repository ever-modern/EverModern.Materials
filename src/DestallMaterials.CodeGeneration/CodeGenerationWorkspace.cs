using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;

namespace DestallMaterials.CodeGeneration;

public sealed class CodeGenerationWorkspace : ICompilationSource, IDisposable
{
    volatile Solution _solution;

    readonly List<Func<IReadOnlyList<CodeFile>, CancellationToken, Task>> _onProjectsChange = new();
    readonly SourceFilesWritingSettings _settings;

    public IReadOnlyDictionary<string, string> ProjectLocations { get; }

    CodeGenerationWorkspace(Solution solution, SourceFilesWritingSettings settings)
    {
        _solution = solution;
        ProjectLocations = solution.Projects.ToDictionary(p => p.Name, p => p?.FilePath ?? throw new FileNotFoundException());
        _settings = settings;
    }

    public static CodeGenerationWorkspace Create(string mainProjectFile, SourceFilesWritingSettings? settings = null)
    {
        var workspace = CreateWorkspace(mainProjectFile);

        var solution = workspace.CurrentSolution;

        return Create(solution, settings ?? SourceFilesWritingSettings.Standard);
    }

    public static CodeGenerationWorkspace Create(Solution solution, SourceFilesWritingSettings? settings = null)
        => new(solution, settings ?? SourceFilesWritingSettings.Standard);

    public static CodeGenerationWorkspace Create(Workspace workspace)
        => Create(workspace.CurrentSolution);

    /// <inheritdoc/>
    public async Task<Compilation> GetProjectCompilationAsync(string projectName, CancellationToken cancellationToken)
    {
        var project = _solution.Projects.FirstOrDefault(p => p.Name == projectName)
            ?? throw new InvalidOperationException($"Project with name {projectName} is not found among projects of the workspace.");

        var result = await project.GetCompilationAsync(cancellationToken);

        return result!;
    }

    /// <summary>
    /// Added a bunch of source files to the workspace.
    /// </summary>
    /// <param name="sourceFiles">Files to add</param>
    /// <returns>Those files, that were not present and have been added to system.</returns>
    public async Task<IReadOnlyList<CodeFile>> AddSourceFilesAsync(IEnumerable<CodeFile> sourceFiles, CancellationToken cancellationToken)
    {
        var processedProjects = await Task.WhenAll(sourceFiles.Select(async sourceFile =>
        {
            var oldProject = _solution.Projects.FirstOrDefault(p => p.Name == sourceFile.Path.ProjectName)
                ?? throw new SourceCodeGenerationException($"Project {sourceFile.Path.ProjectName} has not been found in the solution.");
            if (!sourceFile.IsCharpFile())
            {
                return (oldProject, newProject: oldProject, sourceFile, areDifferent: false);
            }

            var originalPath = sourceFile.Path;
            var finalPath = _settings.TransformFilePathBeforeWriting(sourceFile.Path);

            var replacementFilePath = finalPath with { FileName = originalPath.FileName };
            if (oldProject.Documents.Find(replacementFilePath) is not null)
            {
                return (oldProject, newProject: oldProject, sourceFile, areDifferent: false);
            }

            sourceFile = sourceFile with { Path = finalPath };
            var newProject = await oldProject.WithAsync(sourceFile, cancellationToken);

            bool areDifferent = !ReferenceEquals(newProject, oldProject);

            _solution = newProject.Solution;

            return (oldProject, newProject, sourceFile, areDifferent);
        }));

        var changedProjects = processedProjects
            .Where(pp => pp.areDifferent)
            .ToArray();

        var addedFiles = changedProjects
            .Select(cp => cp.sourceFile)
            .ToArray();

        foreach (var callback in _onProjectsChange)
        {
            await callback(addedFiles, cancellationToken);
        }

        return addedFiles;
    }

    /// <summary>
    /// Subscribe for the case any project gets added source files to.
    /// </summary>
    /// <param name="onProjectDocumentChanged">Callback</param>
    /// <returns>Unsubsription token. Dispose to unsubscribe.</returns>
    public IDisposable SubscribeForSolutionChange(Func<IReadOnlyList<CodeFile>, CancellationToken, Task> onProjectDocumentChanged)
    {
        _onProjectsChange.Add(onProjectDocumentChanged);
        DisposableCallback unsubcribe = new(() => _onProjectsChange.Remove(onProjectDocumentChanged));
        return unsubcribe;
    }

    static AdhocWorkspace CreateWorkspace(string csprojFile)
    {
        string projectToAnalyze = csprojFile;

        if (!File.Exists(projectToAnalyze))
        {
            throw new FileNotFoundException(projectToAnalyze);
        }

        AnalyzerManager manager = new AnalyzerManager();
        IProjectAnalyzer analyzer = manager.GetProject(projectToAnalyze);

        var workspace = new AdhocWorkspace();

        analyzer.AddToWorkspace(workspace, true);

        return workspace;
    }

    public void Dispose()
        => _onProjectsChange.Clear();
}

/// <summary>
/// Server to get compilation of projects besides from those passed to the template.
/// </summary>
public interface ICompilationSource
{
    /// <summary>
    /// Get compilation object of the project within workspace. Cached upon calling, so consequent calls will execute instantly.
    /// </summary>
    /// <param name="projectName">Exact project name to get compilation of</param>
    /// <param name="cancellationToken">Cancellation</param>
    /// <returns>Compilation object</returns>
    Task<Compilation> GetProjectCompilationAsync(string projectName, CancellationToken cancellationToken);
}


file class DisposableCallback : IDisposable
{
    Action OnDisposed { get; }

    internal DisposableCallback(Action onDisposed)
    {
        OnDisposed = onDisposed;
    }

    public static implicit operator DisposableCallback(Action onDisposed)
        => new DisposableCallback(onDisposed);

    public void Dispose() => OnDisposed();
}
