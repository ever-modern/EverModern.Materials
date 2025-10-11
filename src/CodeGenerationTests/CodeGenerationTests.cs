using DestallMaterials.CodeGeneration;
using DestallMaterials.WheelProtection.Linq;

namespace CodeGenerationTests;

public abstract class CodeGenerationTests
{
    protected static readonly string _supplierProject;
    protected static readonly string _consumerProject;

    protected const string nameSpace = "GeneratedNamespace";
    protected const string className = "GeneratedClass";
    protected const string fileName = $"{className}.cs";
    protected const string sourceText = $"namespace {nameSpace} {{ public class {className} {{}} }}";

    protected CodeFile GeneratedFile(string projectName) => new(new(projectName, fileName), sourceText);

    static CodeGenerationTests()
    {
        (_supplierProject, _consumerProject) = TestsPreparation.EnsureSamples();
    }

    protected static Dictionary<string, object> ToTestComponentParameters(CodeFile codeFile) 
        => ("Path", (object)codeFile.Path, "Content", codeFile.Content, "Virtual", codeFile.Virtual).ToDictionary();
}
