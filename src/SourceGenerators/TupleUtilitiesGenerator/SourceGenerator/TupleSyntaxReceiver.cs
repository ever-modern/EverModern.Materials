using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGenerator
{
    public class TupleSyntaxReceiver : ISyntaxReceiver
    {
        readonly HashSet<TupleExpressionSyntax> _tuples;

        public TupleSyntaxReceiver(HashSet<TupleExpressionSyntax> tuples)
        {
            _tuples = tuples;
        }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TupleExpressionSyntax tes)
            {
                _tuples.Add(tes);
            }
        }
    }
}

