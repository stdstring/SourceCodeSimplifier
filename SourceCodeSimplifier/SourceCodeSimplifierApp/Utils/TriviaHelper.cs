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

        public static IList<SyntaxTrivia> ConstructSingleLineCommentsTrivia(SyntaxTriviaList source, SyntaxTrivia prefixTrivia, SyntaxTrivia eolTrivia)
        {
            IList<SyntaxTrivia> destTrivia = new List<SyntaxTrivia>();
            source.Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)).ForEach(comment =>
            {
                destTrivia.Add(prefixTrivia);
                destTrivia.Add(comment);
                destTrivia.Add(eolTrivia);
            });
            return destTrivia;
        }

        public static IList<SyntaxTrivia> ConstructSingleLineCommentsTrivia(SyntaxTriviaList source, Int32 prefixLength, SyntaxTrivia eolTrivia)
        {
            SyntaxTrivia prefixTrivia = SyntaxFactory.Whitespace(new String(' ', prefixLength));
            return ConstructSingleLineCommentsTrivia(source, prefixTrivia, eolTrivia);
        }

        public static SyntaxTriviaList ConstructLeadingTrivia(SyntaxTriviaList sourceTrivia, SyntaxTrivia leadingSpaceTrivia, SyntaxTrivia eolTrivia)
        {
            IList<SyntaxTrivia> comments = ConstructSingleLineCommentsTrivia(sourceTrivia, leadingSpaceTrivia, eolTrivia);
            comments.Add(leadingSpaceTrivia);
            return new SyntaxTriviaList(comments);
        }
    }
}