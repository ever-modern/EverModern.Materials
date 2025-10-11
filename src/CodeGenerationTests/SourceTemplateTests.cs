using DestallMaterials.CodeGeneration;
using DestallMaterials.WheelProtection.Extensions.Tasks;
using TestComponents;


namespace CodeGenerationTests;

[Parallelizable(scope: ParallelScope.All)]
[TestFixture]
public class SourceTemplateTests : CodeGenerationTests
{
    [Test]
    public async Task RenderFile_WithContent()
    {
        // Arrange
        using var workspace = CodeGenerationWorkspace.Create(_consumerProject);
        var renderer = new SourceFileRenderer(workspace, sp => { });
        var mainProjectName = workspace.ProjectLocations.Keys.First(_consumerProject.Contains);

        var parameters1 = ToTestComponentParameters(GeneratedFile(mainProjectName));

        parameters1["Virtual"] = true;
        var parameters2 = parameters1.ToDictionary();

        parameters2["Virtual"] = false;
        parameters2["Content"] = parameters2["Content"] + "\nnamespace ObjectiveCSharp {}";
        parameters2["Path"] = (ProjectRelativeFilePath)parameters2["Path"] with { FileName = "AnotherFile.cs" };

        var sourceWriter = new SourceFileWriter(workspace, renderer, SourceFilesWritingSettings.Standard);

        // Act
        var (renderResult_1, renderResult_2) = await (
                sourceWriter.AddFilesToWorkspaceAsync<TestCodegenComponent>(parameters1, default), 
                sourceWriter.AddFilesToWorkspaceAsync<TestCodegenComponent>(parameters2, default)
            );

        // Assert
        var compilation = await workspace.GetProjectCompilationAsync(mainProjectName, default);
        var createdClassSymbol = compilation.GetTypeByMetadataName($"{nameSpace}.{className}");
        
        Assert.IsNotNull(createdClassSymbol);
        Assert.IsTrue(renderResult_1.Single().Virtual);
        Assert.IsFalse(renderResult_2.Single().Virtual);
    }
}