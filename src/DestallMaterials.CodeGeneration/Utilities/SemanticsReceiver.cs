using DestallMaterials.WheelProtection.Extensions.Collections;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;

using SymbolsDict = System.Collections.Generic.IReadOnlyDictionary<Microsoft.CodeAnalysis.INamedTypeSymbol, System.Collections.Generic.IReadOnlyList<Microsoft.CodeAnalysis.ISymbol>>;

namespace DestallMaterials.CodeGeneration.Utilities;

public class SemanticsReceiver : CSharpSyntaxWalker
{
    readonly Dictionary<string, List<ISymbol>> _attributeTiedSymbols;
    readonly Compilation _compilation;

    readonly ConcurrentDictionary<string, SemanticModel> _semanticModels = new();

    public SymbolsDict AttributeTiedSymbols
        => _attributeTiedSymbols.ToDictionary(
            kv => _compilation.GetTypeByMetadataName(kv.Key) 
                ?? throw new SourceCodeGenerationException($"Compilation {_compilation} does not recognize type {kv.Key}"), 
            kv => kv.Value.AsReadOnlyList());

    internal SemanticsReceiver(Compilation compilation, IEnumerable<string> seekedAttributes)
    {
        _compilation = compilation;
        _attributeTiedSymbols = new();

        foreach (var attr in seekedAttributes)
        {
            _attributeTiedSymbols.Add(attr, new());
        }
    }

    void OnVisitSyntaxNode(SyntaxNode syntaxNode, SemanticModel treeSemanticModel)
    {
        var soughtAttributes = _attributeTiedSymbols.Keys;
        if (syntaxNode is ClassDeclarationSyntax cds)
        {
            if (cds.AttributeLists.Count > 0 || cds.BaseList?.Types.Any() == true)
            {
                var classSymbol = treeSemanticModel.GetDeclaredSymbol(cds)
                    ?? throw new InvalidOperationException();

                foreach (var attr in soughtAttributes.Where(a => classSymbol.RelatesToAttribute(a)))
                {
                    _attributeTiedSymbols[attr].Add(classSymbol);
                }
            }
        }
        else if (syntaxNode is MethodDeclarationSyntax mds && mds.DescendantNodesAndTokens().Any(t => t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword)))
        {
            var methodSymbol = treeSemanticModel.GetDeclaredSymbol(mds);

            var presentAttributes = soughtAttributes.Where(attr =>
            {
                var hasAttribute = methodSymbol.HasAttribute(attr);
                if (hasAttribute)
                {
                    return true;
                }

                var parameters = methodSymbol.Parameters;

                return parameters.Any(p => p.Type.RelatesToAttribute(attr));
            });

            foreach (var attr in presentAttributes)
            {
                _attributeTiedSymbols[attr].Add(methodSymbol);
            }
        }
    }

    public override void Visit(SyntaxNode? node)
    {
        base.Visit(node);

        if (node is null)
        {
            return;
        }

        var tree = node.SyntaxTree;

        if (!_compilation.ContainsSyntaxTree(tree))
        {
            throw new InvalidOperationException("Visited syntax node must belong to the receiver's compilation.");
        }

        SemanticModel semanticModel;
        if (!_semanticModels.TryGetValue(tree.FilePath, out semanticModel))
        {
            semanticModel = _compilation.GetSemanticModel(tree);
            _semanticModels[tree.FilePath] = semanticModel;
        }

        OnVisitSyntaxNode(node, semanticModel);
    }

    public SymbolsDict VisitAllSyntaxTrees()
    {
        _compilation.SyntaxTrees.Select(tree =>
        {
            Visit(tree.GetRoot());
            return true;
        }).ToList();

        return AttributeTiedSymbols;
    }

    /// <summary>
    /// Visits all syntax trees synchronously
    /// </summary>
    /// <param name="compilation"></param>
    /// <param name="soughtAttributes"></param>
    /// <returns>Symbols bearing the attributes specified</returns>
    public static SymbolsDict AnalyzeAttributeBearers(
        Compilation compilation,
        IEnumerable<string> soughtAttributes)
    {
        var semanticsReceiver = new SemanticsReceiver(compilation, soughtAttributes);
        var result = semanticsReceiver.VisitAllSyntaxTrees();
        return result;
    }
}

public static class SemanticsReceiverExtensions
{
    public static IReadOnlyList<ISymbol> GetAttributeSymbols<TAttribute>(this SymbolsDict symbolsDict)
        => symbolsDict.GetAttributeSymbols(typeof(TAttribute).FullName);

    public static IReadOnlyList<ISymbol> GetAttributeSymbols(this SymbolsDict symbolsDict, string attributeFullName)
        => symbolsDict.FirstOrDefault(kv => kv.Key.ToFullDisplayString() == attributeFullName).Value ?? Array.Empty<ISymbol>();
}

public static class SyntaxExtensions
{

}
