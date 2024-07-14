using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceCodeSimplifierApp.Utils
{
    internal static class SyntaxTreeHelper
    {
        public static StatementSyntax? FindParentStatement(this SyntaxNode node)
        {
            SyntaxNode? current = node.Parent;
            while (current != null)
            {
                if (current is StatementSyntax statement)
                    return statement;
                current = current.Parent;
            }
            return null;
        }
    }
}
