using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EverModern.SyntaxGenerator
{
    public class TupleSyntaxReceiver : ISyntaxReceiver
    {
        public HashSet<TupleExpressionSyntax> Tuples { get; } = new HashSet<TupleExpressionSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TupleExpressionSyntax tes)
            {
                Tuples.Add(tes);
            }
        }
    }
}

