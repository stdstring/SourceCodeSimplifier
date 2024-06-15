using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SourceCodeSimplifierApp.Utils
{
    internal static class TriviaHelper
    {
        public static SyntaxTrivia GetLeadingSpaceTrivia(SyntaxNode node)
        {
            IReadOnlyList<SyntaxTrivia> leadingTrivia = node.GetLeadingTrivia();
            int lastEndOfLineIndex = leadingTrivia.Count - 1;
            while (lastEndOfLineIndex >= 0)
            {
                if (leadingTrivia[lastEndOfLineIndex].IsKind(SyntaxKind.EndOfLineTrivia))
                    break;
                --lastEndOfLineIndex;
            }
            int totalSpaceSize = 0;
            for (int index = lastEndOfLineIndex + 1; index < leadingTrivia.Count; ++index)
                totalSpaceSize += leadingTrivia[index].Span.Length;
            return SyntaxFactory.Whitespace(new string(' ', totalSpaceSize));
        }

        public static SyntaxTrivia GetTrailingEndOfLineTrivia(SyntaxNode node)
        {
            SyntaxTrivia endOfLineTrivia = node.GetTrailingTrivia().FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));
            return endOfLineTrivia.IsKind(SyntaxKind.EndOfLineTrivia) ? endOfLineTrivia : SyntaxFactory.EndOfLine(Environment.NewLine);
        }
    }
}