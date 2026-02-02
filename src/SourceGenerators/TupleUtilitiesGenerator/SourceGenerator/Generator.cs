using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EverModern.SyntaxGenerator
{
    [Generator]
    public class SourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx =>
            {
                ctx.AddSource($"UtilityTypes.cs", UtilityTypes.All.Select(t => t.Code).Merge("\n"));
            });

            var tupleExpressions = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => node is TupleExpressionSyntax,
                    transform: static (ctx, _) => (TupleExpressionSyntax)ctx.Node)
                .Collect();

            context.RegisterSourceOutput(tupleExpressions, (ctx, tuples) =>
            {
                try
                {
                    var tupleSyntaxes = new HashSet<TupleExpressionSyntax>(tuples);
                    var result = tupleSyntaxes.GenerateExtensionClass();

                    var code = result.ToString();
                    ctx.AddSource($"DestallTupleExtensions.cs", code);
                    //File.WriteAllText("DestallTupleExtensions.artifact", $"{code}");

                    //ctx.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("SG001", "Success", "Success", "Success", DiagnosticSeverity.Error, true), Location.None));
                }
                catch (System.Exception e)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("SG001", "Error", $"{e}", "Error", DiagnosticSeverity.Error, true), Location.None));
                    File.WriteAllText("artifact.tt", $"{e}");
                }
            });
        }
    }
}

