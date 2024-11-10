using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.MSBuild;
using NUnit.Framework;

namespace SourceCodeSimplifierConceptTests
{
    [TestFixture]
    public class ConceptTests
    {
        [Explicit]
        [Test]
        public void Transform()
        {
            const String projectFilename = "./SourceCodeSimplifierConceptSource/SourceCodeSimplifierConceptSource.csproj";
            DirectoryUtils.CopyDirectory("../../../../SourceCodeSimplifierConceptSource", "./SourceCodeSimplifierConceptSource", true);
            PrerequisitesManager.Run();
            ProjectProcessor projectProcessor = new ProjectProcessor(projectFilename);
            projectProcessor.Process();
        }
    }

    internal class ProjectProcessor
    {
        public ProjectProcessor(String projectFilename)
        {
            _projectFilename = projectFilename;
        }

        public void Process()
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Project project = workspace.OpenProjectAsync(_projectFilename).Result;
            if (project.FilePath == null)
                throw new InvalidOperationException();
            Compilation? compilation = project.GetCompilationAsync().Result;
            if (compilation == null)
                throw new InvalidOperationException();
            if (!CompilationChecker.CheckCompilationErrors(project.FilePath, compilation))
                throw new InvalidOperationException();
            NameOfTransformer nameOfTransformer = new NameOfTransformer();
            ObjectInitializerTransformer objectInitializerTransformer = new ObjectInitializerTransformer();
            DocumentId[] documentIds = project.Documents
                .Where(doc => doc.SourceCodeKind == SourceCodeKind.Regular)
                .Select(doc => doc.Id)
                .ToArray();
            foreach (DocumentId documentId in documentIds)
            {
                Document? sourceDocument = project.GetDocument(documentId);
                if (sourceDocument == null)
                    throw new InvalidOperationException();
                Document documentStage1 = nameOfTransformer.Transform(sourceDocument);
                Document documentStage2 = objectInitializerTransformer.Transform(documentStage1);
                project = documentStage2.Project;
            }
            workspace.TryApplyChanges(project.Solution);
        }

        private readonly String _projectFilename;
    }

    internal class NameOfTransformer
    {
        public Document Transform(Document sourceDocument)
        {
            SyntaxTree? syntaxTree = sourceDocument.GetSyntaxTreeAsync().Result;
            if (syntaxTree == null)
                throw new InvalidOperationException();
            SemanticModel? semanticModel = sourceDocument.GetSemanticModelAsync().Result;
            if (semanticModel == null)
                throw new InvalidOperationException();
            NameOfSyntaxRewriter rewriter = new NameOfSyntaxRewriter(semanticModel);
            SyntaxNode sourceRoot = syntaxTree.GetRoot();
            SyntaxNode destRoot = rewriter.Visit(sourceRoot);
            Document destDocument = sourceDocument.WithSyntaxRoot(destRoot);
            return destDocument;
        }

        private class NameOfSyntaxRewriter : CSharpSyntaxRewriter
        {
            public NameOfSyntaxRewriter(SemanticModel model)
            {
                _model = model;
            }

            public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                switch (node.Expression)
                {
                    case IdentifierNameSyntax {Identifier.Text: "nameof"}:
                        SymbolInfo symbolInfo = _model.GetSymbolInfo(node);
                        if (symbolInfo.Symbol is null)
                        {
                            String name = node.ArgumentList.Arguments.First().Expression.ToString();
                            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(name))
                                .WithLeadingTrivia(node.GetLeadingTrivia())
                                .WithTrailingTrivia(node.GetTrailingTrivia());
                        }
                        break;
                }
                return node;
            }

            private readonly SemanticModel _model;
        }
    }

    internal class ObjectInitializerTransformer
    {
        public Document Transform(Document sourceDocument)
        {
            // create DocumentEditor
            DocumentEditor documentEditor = DocumentEditor.CreateAsync(sourceDocument).Result;
            // make change
            SyntaxNode sourceRoot = sourceDocument.GetSyntaxRootAsync().Result!;
            foreach (ObjectCreationExpressionSyntax objExpression in sourceRoot.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                if (objExpression.Initializer == null)
                    continue;
                switch (objExpression.Parent)
                {
                    case AssignmentExpressionSyntax {Parent: InitializerExpressionSyntax}:
                        break;
                    case AssignmentExpressionSyntax {Parent: ExpressionStatementSyntax expressionStatement} assignmentExpression:
                    {
                        SyntaxTrivia leadingTrivia = TriviaHelper.GetLeadingSpaceTrivia(expressionStatement);
                        SyntaxTrivia trailingTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(expressionStatement);
                        ArgumentListSyntax newArgs = objExpression.ArgumentList ?? SyntaxFactory.ArgumentList();
                        ObjectCreationExpressionSyntax newObjExpression = SyntaxFactory.ObjectCreationExpression(objExpression.Type, newArgs, null);
                        ExpressionSyntax assignmentLeft = assignmentExpression.Left;
                        AssignmentExpressionSyntax newAssignmentExpression = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            assignmentLeft,
                            newObjExpression);
                        ExpressionStatementSyntax newExpressionStatement = SyntaxFactory.ExpressionStatement(newAssignmentExpression)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            .NormalizeWhitespace()
                            .WithLeadingTrivia(leadingTrivia)
                            .WithTrailingTrivia(trailingTrivia);
                        IList<StatementSyntax> dest = new List<StatementSyntax>{newExpressionStatement};
                        CollectObjectInitializerExpressions(assignmentLeft, objExpression.Initializer, leadingTrivia, trailingTrivia, dest);
                        ReplaceStatement(documentEditor, expressionStatement, dest);
                        break;
                    }
                    case EqualsValueClauseSyntax {Parent: VariableDeclaratorSyntax {Parent: VariableDeclarationSyntax {Parent: LocalDeclarationStatementSyntax localDeclarationStatement}}}:
                    {
                        VariableDeclarationSyntax variableDeclaration = localDeclarationStatement.Declaration;
                        if (variableDeclaration.Variables.Count > 1)
                            throw new NotSupportedException("More that one variable declarations is not supported now");
                        VariableDeclaratorSyntax variableDeclarator = variableDeclaration.Variables[0];
                        SyntaxToken identifier = variableDeclarator.Identifier;
                        SyntaxTrivia leadingTrivia = TriviaHelper.GetLeadingSpaceTrivia(localDeclarationStatement);
                        SyntaxTrivia trailingTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(localDeclarationStatement);
                        ArgumentListSyntax newArgs = objExpression.ArgumentList ?? SyntaxFactory.ArgumentList();
                        ObjectCreationExpressionSyntax newObjExpression = SyntaxFactory.ObjectCreationExpression(objExpression.Type, newArgs, null);
                        EqualsValueClauseSyntax newEqualsValueClause = SyntaxFactory.EqualsValueClause(newObjExpression);
                        VariableDeclaratorSyntax newVariableDeclarator = SyntaxFactory.VariableDeclarator(identifier, null, newEqualsValueClause);
                        SeparatedSyntaxList<VariableDeclaratorSyntax> newVariableDeclarators = SyntaxFactory.SeparatedList(new[]{newVariableDeclarator});
                        VariableDeclarationSyntax newVariableDeclaration = SyntaxFactory.VariableDeclaration(variableDeclaration.Type, newVariableDeclarators);
                        LocalDeclarationStatementSyntax newLocalDeclarationStatement = SyntaxFactory.LocalDeclarationStatement(newVariableDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            .NormalizeWhitespace()
                            .WithLeadingTrivia(leadingTrivia)
                            .WithTrailingTrivia(trailingTrivia);
                        IList<StatementSyntax> dest = new List<StatementSyntax>{newLocalDeclarationStatement};
                        CollectObjectInitializerExpressions(SyntaxFactory.IdentifierName(identifier), objExpression.Initializer, leadingTrivia, trailingTrivia, dest);
                        ReplaceStatement(documentEditor, localDeclarationStatement, dest);
                        break;
                    }
                    default:
                        throw new InvalidOperationException();
                }
            }
            // get changed document
            Document destDocument = documentEditor.GetChangedDocument();
            return destDocument;
        }

        private void CollectObjectInitializerExpressions(ExpressionSyntax baseLeftAssignment,
                                                         InitializerExpressionSyntax initializerExpression,
                                                         SyntaxTrivia leadingTrivia,
                                                         SyntaxTrivia trailingTrivia,
                                                         IList<StatementSyntax> dest)
        {
            foreach (ExpressionSyntax expression in initializerExpression.Expressions)
            {
                switch (expression)
                {
                    case AssignmentExpressionSyntax {Left: SimpleNameSyntax name, Right: ObjectCreationExpressionSyntax objCreationExpression}:
                    {
                        MemberAccessExpressionSyntax leftAssignment = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, baseLeftAssignment, name);
                        ArgumentListSyntax arguments = objCreationExpression.ArgumentList ?? SyntaxFactory.ArgumentList();
                        ObjectCreationExpressionSyntax rightAssignment = SyntaxFactory.ObjectCreationExpression(objCreationExpression.Type, arguments, null);
                        AssignmentExpressionSyntax resultAssignment = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, leftAssignment, rightAssignment);
                        ExpressionStatementSyntax resultStatement = SyntaxFactory.ExpressionStatement(resultAssignment)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            .NormalizeWhitespace()
                            .WithLeadingTrivia(leadingTrivia)
                            .WithTrailingTrivia(trailingTrivia);
                        dest.Add(resultStatement);
                        if (objCreationExpression.Initializer != null)
                            CollectObjectInitializerExpressions(leftAssignment, objCreationExpression.Initializer, leadingTrivia, trailingTrivia, dest);
                        break;
                    }
                    case AssignmentExpressionSyntax {Left: SimpleNameSyntax name, Right: var rightAssignment}:
                    {
                        MemberAccessExpressionSyntax leftAssignment = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, baseLeftAssignment, name);
                        AssignmentExpressionSyntax resultAssignment = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, leftAssignment, rightAssignment);
                        ExpressionStatementSyntax resultStatement = SyntaxFactory.ExpressionStatement(resultAssignment)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            .NormalizeWhitespace()
                            .WithLeadingTrivia(leadingTrivia)
                            .WithTrailingTrivia(trailingTrivia);
                        dest.Add(resultStatement);
                            break;
                    }
                    default:
                        throw new NotSupportedException("Non AssignmentExpressionSyntax isn't supported now");
                }
            }
        }

        private void ReplaceStatement(DocumentEditor documentEditor, StatementSyntax oldStatement, IList<StatementSyntax> newStatements)
        {
            switch (oldStatement.Parent)
            {
                case null:
                    throw new NotSupportedException("Root (without parent) statements isn't support now");
                case BlockSyntax:
                    documentEditor.InsertAfter(oldStatement, newStatements);
                    documentEditor.RemoveNode(oldStatement);
                    break;
                default:
                    SyntaxTrivia leadingTrivia = TriviaHelper.GetLeadingSpaceTrivia(oldStatement.Parent);
                    SyntaxTrivia trailingTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(oldStatement.Parent);
                    BlockSyntax block = SyntaxFactory.Block(newStatements)
                        .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithTrailingTrivia(trailingTrivia))
                        .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(leadingTrivia))
                        .WithLeadingTrivia(leadingTrivia)
                        .WithTrailingTrivia(trailingTrivia);
                    documentEditor.ReplaceNode(oldStatement, block);
                    break;
            }
        }
    }

    internal static class TriviaHelper
    {
        public static SyntaxTrivia GetLeadingSpaceTrivia(SyntaxNode node)
        {
            IReadOnlyList<SyntaxTrivia> leadingTrivia = node.GetLeadingTrivia();
            Int32 lastEndOfLineIndex = leadingTrivia.Count - 1;
            while (lastEndOfLineIndex >= 0)
            {
                if (leadingTrivia[lastEndOfLineIndex].IsKind(SyntaxKind.EndOfLineTrivia))
                    break;
                --lastEndOfLineIndex;
            }
            Int32 totalSpaceSize = 0;
            for (Int32 index = lastEndOfLineIndex + 1; index < leadingTrivia.Count; ++index)
                totalSpaceSize += leadingTrivia[index].Span.Length;
            return SyntaxFactory.Whitespace(new String(' ', totalSpaceSize));
        }

        public static SyntaxTrivia GetTrailingEndOfLineTrivia(SyntaxNode node)
        {
            SyntaxTrivia endOfLineTrivia = node.GetTrailingTrivia().FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));
            return endOfLineTrivia.IsKind(SyntaxKind.EndOfLineTrivia) ? endOfLineTrivia : SyntaxFactory.EndOfLine(Environment.NewLine);
        }
    }
}
