using Microsoft.CodeAnalysis;
using DestallMaterials.WheelProtection.Extensions.Objects;
using DestallMaterials.WheelProtection.Extensions.Tasks;
using Microsoft.CodeAnalysis.CSharp;

namespace DestallMaterials.CodeGeneration;

public static class CodegenWorkspaceExtensions
{


    /// <summary>
    /// Subscribe for particular project changes
    /// </summary>
    /// <param name="codegenSystem">Code generation system</param>
    /// <param name="projectName">Project to whose changes to subscribe</param>
    /// <param name="callback">Callback</param>
    /// <returns>Unsibscription token</returns>
    public static IDisposable SubscribeForProjectChange(this CodeGenerationWorkspace codegenSystem, string projectName, Func<IReadOnlyList<CodeFile>, CancellationToken, Task> callback)
        => codegenSystem.SubscribeForSolutionChange((changes, ct) =>
        {
            var relevantChanges = changes.Where(c => c.Path.ProjectName == projectName).ToArray();
            if (relevantChanges.Length == 0)
            {
                return Task.CompletedTask;
            }
            return callback(relevantChanges, ct);
        });

    /// <summary>
    /// Add one source file to system.
    /// </summary>
    /// <param name="codeGenerationSystem">System</param>
    /// <param name="sourceFile">File to add</param>
    /// <returns>Whether the file has been added or not</returns>
    public static Task<bool> AddSourceFileAsync(this CodeGenerationWorkspace codeGenerationSystem, CodeFile sourceFile, CancellationToken cancellationToken)
        => codeGenerationSystem.AddSourceFilesAsync(sourceFile.Yield(), cancellationToken).Then(files => files.Any());

    /// <summary>
    /// Add a source file to code generation workspace
    /// </summary>
    /// <param name="codeGenerationWorkspace"></param>
    /// <param name="projectName"></param>
    /// <param name="fileName"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public static Task<bool> AddSourceFileAsync(
        this CodeGenerationWorkspace codeGenerationWorkspace,
        string projectName,
        string fileName,
        string code,
        CancellationToken cancellationToken)
        => codeGenerationWorkspace.AddSourceFileAsync(new CodeFile(new ProjectRelativeFilePath(projectName, fileName), code), cancellationToken);


    /// <summary>
    /// Compose file system path for project-based path.
    /// </summary>
    /// <param name="sourceFilePath"></param>
    /// <returns></returns>
    public static string ToAbsolutePath(this CodeGenerationWorkspace workspace, ProjectRelativeFilePath sourceFilePath)
    {
        var projectName = sourceFilePath.ProjectName;

        var projectDirectory = Directory.GetParent(workspace.ProjectLocations[projectName])?.FullName
            ?? throw new DirectoryNotFoundException();

        sourceFilePath = sourceFilePath with { ProjectName = projectDirectory };

        var result = sourceFilePath.ToString();

        if (Path.DirectorySeparatorChar != '/')
        {
            result = result.Replace('/', Path.DirectorySeparatorChar);
        }

        return result;
    }
}
