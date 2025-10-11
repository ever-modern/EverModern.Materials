using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator;
using Xunit;

namespace SourceGeneratorTests;

public class Tests
{
    const string ExtensionNamespace = TupleCodeGeneration.ExtensionNamespace;

    [Fact]
    void ExtensionsSyntax()
    {
        var manyTypes = "decimal int byte string short ushort uint long ulong bool System.DateTime char int[] string[]"
            .Split(' ');

        string source = $@"
using {ExtensionNamespace};
using System.Threading.Tasks;
namespace Foo
{{
    class C
    {{
        void M()
        {{
            Task.Run(async () => 
            {{
                var tuple = (10, ""Car"");
                var tasks = (Task.Delay(10), Task.FromResult(500));
                var tasks2 = (Task.FromResult(900), Task.FromResult(900));

                var (_, number) = await tasks;
                var (number1, number2) = tasks2;

                var ({manyTypes.Select((_, i) => $"i{i}").Merge()})
                    = await ({manyTypes.Select((t) => $"Task.FromResult(default({t}))").Merge()});

                System.Console.WriteLine(tuple);

                var dict = (
    1, 5,
    2, 10,
    3, 15
).ToDictionary();
                
            }}).Wait();
        }}
    }}
}}";

        var diagnostics = GetGeneratedOutput(source).OrderByDescending(d => d.Severity).ToArray();

        Assert.Empty(diagnostics);

        //if (diagnostics.Length > 0)
        //{
        //    Console.ForegroundColor = ConsoleColor.Red;
        //    Console.WriteLine("Diagnostics:");
        //    foreach (var diag in diagnostics)
        //    {
        //        Console.WriteLine("   " + diag.ToString());
        //    }
        //    Console.WriteLine();
        //    Console.WriteLine("Output:");
        //}
        //else
        //{
        //    Console.ForegroundColor = ConsoleColor.Green;
        //    Console.WriteLine("No errors!");
        //}
    }

    static IEnumerable<TupleExpressionSyntax> GetTupleExpressionSyntaxes(SyntaxNode syntaxNode)
    {
        if (syntaxNode is TupleExpressionSyntax tupleExpressionSyntax)
        {
            yield return tupleExpressionSyntax;
        }

        foreach (var node in syntaxNode.DescendantNodes())
        {
            foreach (var result in GetTupleExpressionSyntaxes(node))
            {
                yield return result;
            }
        }
    }

    private static ImmutableArray<Diagnostic> GetGeneratedOutput(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>();
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic)
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: "foo",
            syntaxTrees: [tree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(UtilityTypes.All.Select(t => t.Code).Merge("\n")));

        var tupleSyntaxes = GetTupleExpressionSyntaxes(tree.GetRoot());

        // TODO: Uncomment these lines if you want to return immediately if the injected program isn't valid _before_ running generators
        //
        // ImmutableArray<Diagnostic> compilationDiagnostics = compilation.GetDiagnostics();
        //
        // if (diagnostics.Any())
        // {
        //     return (diagnostics, "");
        // }

        var extensionsClass = tupleSyntaxes
            .DistinctBy(s => s.ToFullString())
            .GenerateExtensionClass(compilation)
            .ToString();

        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(extensionsClass));

        File.WriteAllText("artifact.cs", extensionsClass);

        return compilation.GetDiagnostics();
    }
}