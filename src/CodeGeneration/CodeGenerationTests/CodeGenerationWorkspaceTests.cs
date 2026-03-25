using Microsoft.CodeAnalysis;
using System.Diagnostics;
using EverModern.CodeGeneration;
using EverModern.WheelProtection.Extensions.Enumerables;
using EverModern.WheelProtection.Extensions.Strings;
using EverModern.WheelProtection.Extensions.Tasks;
using EverModern.WheelProtection.Extensions.Ranges;

namespace CodeGenerationTests;

[Parallelizable(scope: ParallelScope.All)]
[TestFixture]
public class CodeGenerationWorkspaceTests : CodeGenerationTests
{
    [Test]
    public async Task AddSource_MustAdd_TreeMustBeFound()
    {
        // Arrange
        var system = CodeGenerationWorkspace.Create(_consumerProject);
        var projectName = system.ProjectLocations.Keys.First(pn => pn.Contains("Consumer"));

        // Act
        var sourceFile = new CodeFile(new ProjectRelativeFilePath(projectName, fileName), sourceText);
        await system.AddSourceFilesAsync([sourceFile], default);
        var compilation = await system.GetProjectCompilationAsync(projectName, default);

        // Assert
        var containsClass = compilation.GetTypeByMetadataName($"{nameSpace}.{className}") is not null;
        Assert.True(containsClass);
    }

    [Test]
    public async Task AddSource_ToSecondaryProject_MustBeFound_InMainCompilation()
    {
        // Arrange
        var system = CodeGenerationWorkspace.Create(_consumerProject);
        var mainProjectName = system.ProjectLocations.Keys.First(pn => pn.Contains("Consumer"));
        var secondaryProjectName = system.ProjectLocations.Keys.First(pn => pn.Contains("Supplier"));

        // Act
        var sourceFile = new CodeFile(new ProjectRelativeFilePath(secondaryProjectName, fileName), sourceText);
        await system.AddSourceFilesAsync([sourceFile], default);
        var compilation = await system.GetProjectCompilationAsync(mainProjectName, default);

        // Assert
        var containsClass = compilation.GetTypeByMetadataName($"{nameSpace}.{className}") is not null;
        Assert.True(containsClass);
    }

    [Test]
    public async Task SecondaryProjectClass_MustBeFound_InMainProject()
    {
        // Arrange
        var system = CodeGenerationWorkspace.Create(_consumerProject);
        var mainProjectName = system.ProjectLocations.Keys.First(pn => pn.Contains("Consumer"));
        var secondaryProjectName = system.ProjectLocations.Keys.First(pn => pn.Contains("Supplier"));

        // Act
        var (mainCompilation, secondaryCompilation) = await (
            system.GetProjectCompilationAsync(mainProjectName, default),
            system.GetProjectCompilationAsync(secondaryProjectName, default));

        string classToFind = $"{secondaryProjectName}.SupplierModel";

        // Assert
        var secondaryContains = secondaryCompilation.GetTypeByMetadataName(classToFind) is not null;
        var mainContains = mainCompilation.GetTypeByMetadataName(classToFind) is not null;

        Assert.True(secondaryContains);
        Assert.True(mainContains);
    }

    [Test]
    public async Task Subscribe_SubscriptionMustWorkOnce()
    {
        // Arrange
        using var system = CodeGenerationWorkspace.Create(_consumerProject);
        var mainProjectName = system.ProjectLocations.Keys.First(pn => pn.Contains("Consumer"));
        var secondaryProjectName = system.ProjectLocations.Keys.First(pn => pn.Contains("Supplier"));

        // Act
        system.SubscribeForProjectChange(secondaryProjectName, async (change, ct) =>
        {
            var (_, content, _) = change.Single();

            var classesCount = content.Split(" class ").Length - 1;
            await system.AddSourceFileAsync(mainProjectName, fileName, $@"namespace {nameSpace} 
{{
    public class {className}
    {{
        public const int ClassesCount = {classesCount};
    }}
}}", ct);
        });

        const int newClassesCount = 3;

        await system.AddSourceFileAsync(secondaryProjectName, "newclasses.cs", $@"namespace {nameSpace} 
{{
    {(0..newClassesCount).Select(i => $"public class Class{i} {{}}").Merge("\n")}
}}", default);

        var mainCompilation = await system.GetProjectCompilationAsync(mainProjectName, default);

        // Assert
        var classesCounterClass = mainCompilation.GetTypeByMetadataName($"{nameSpace}.{className}");

        Assert.IsNotNull(classesCounterClass);

        var classesCount = (int)classesCounterClass.GetMembers().OfType<IFieldSymbol>().First().ConstantValue!;

        Assert.AreEqual(newClassesCount, classesCount);
    }

    [Test]
    public async Task Subscribe_IncrementalSubscriptionMustWork()
    {
        // Arrange
        using var system = CodeGenerationWorkspace.Create(_consumerProject);
        var mainProjectName = system.ProjectLocations.Keys.First(pn => pn.Contains("Consumer"));
        var secondaryProjectName = system.ProjectLocations.Keys.First(pn => pn.Contains("Supplier"));

        const string structName = "NewStruct";

        // Act
        system.SubscribeForProjectChange(secondaryProjectName, async (change, ct) =>
        {
            var (_, content, _) = change.Single();

            var classesCount = content.Split(" class ").Length - 1;
            if (classesCount > 0)
            {
                await system.AddSourceFileAsync(mainProjectName, fileName, $@"namespace {nameSpace} 
{{
    public class {className}
    {{
        public const int ClassesCount = {classesCount};
    }}
}}", default);
            }
        });

        system.SubscribeForProjectChange(mainProjectName, async (change, ct) =>
        {
            var (_, content, _) = change.Single();

            var mainCompilation = await system.GetProjectCompilationAsync(mainProjectName, default);

            await system.AddSourceFileAsync(secondaryProjectName, "newstruct.cs", $@"namespace {nameSpace} 
{{
    public struct {structName}
    {{
    }}
}}", ct);
        });

        const int newClassesCount = 3;

        await system.AddSourceFileAsync(secondaryProjectName, "newclasses.cs", $@"namespace {nameSpace} 
{{
    {(0..newClassesCount).Select(i => $"public class Class{i} {{}}").Merge("\n")}
}}", default);

        var (mainCompilation, secondaryCompilation) = await (
            system.GetProjectCompilationAsync(mainProjectName, default),
            system.GetProjectCompilationAsync(secondaryProjectName, default));

        // Assert
        var newStruct = secondaryCompilation.GetTypeByMetadataName($"{nameSpace}.{structName}");
        Assert.IsNotNull(newStruct);
    }

    [Test]
    public async Task RunIntegrated()
    {
        var ticker = Stopwatch.StartNew();

        var system = CodeGenerationWorkspace.Create(_consumerProject);

        const string mainProjectName = "CodegenSample.Consumer";

        var mainCompilation1 = await system.GetProjectCompilationAsync(mainProjectName, default);

        var errors1 = GetErrors(mainCompilation1);

        var referredProjects = system.ProjectLocations.Keys.Where(pn => pn != mainProjectName).ToArray();

        var newClasses = referredProjects.Select(project =>
        {
            var name = project;

            var addedClass = name.Split('.')[^1] + "Class";
            var newClassCode = $@"namespace {name}; public class {addedClass} {{}}";

            addedClass = name + "." + addedClass;

            return (project, addedClass, newClassCode);
        }).ToArray();

        var newClassCode = $@"public class NewClass 
{{
   {newClasses.Select((nc, i) => $"public {nc.addedClass} Prop_{i} {{ get; set; }}").Merge("\n")} 
}}";

        await system.AddSourceFileAsync(mainProjectName, "newClass.cs", newClassCode, default);

        var mainCompilation2 = await system.GetProjectCompilationAsync(mainProjectName, default);

        var errors2 = GetErrors(mainCompilation2);

        var newFiles = newClasses.Select(nc => new CodeFile(new ProjectRelativeFilePath(nc.project, "newclass.cs"), nc.newClassCode));

        await system.AddSourceFilesAsync(newFiles, default);

        var syntaxTrees = await referredProjects.Select(async rp =>
        {
            var compilation = await system.GetProjectCompilationAsync(rp, default);
            return compilation.SyntaxTrees.ToArray();
        }).WhenAll();

        var mainCompilation3 = await system.GetProjectCompilationAsync(mainProjectName, default);

        var errors3 = GetErrors(mainCompilation3);

        var mainSyntaxTree = mainCompilation3.SyntaxTrees;

        Assert.IsFalse(errors1.Any());
        Assert.IsTrue(errors2.Any());
        Assert.IsFalse(errors3.Any());
    }

    static Diagnostic[] GetErrors(Compilation compilation)
        => [.. compilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)];
}