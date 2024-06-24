using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using SourceCodeSimplifierApp.Config;
using SourceCodeSimplifierApp.Output;
using SourceCodeSimplifierApp.Utils;

namespace SourceCodeSimplifierApp.Transformers
{
    internal class ObjectInitializerExprTransformer : ITransformer
    {
        public const String Name = "SourceCodeSimplifierApp.Transformers.ObjectInitializerExprTransformer";

        public ObjectInitializerExprTransformer(IOutput output, TransformerState transformerState)
        {
            _output = output;
            _transformerState = transformerState;
        }

        public Document Transform(Document source)
        {
            if (_transformerState == TransformerState.Off)
                return source;
            _output.WriteInfoLine($"Execution of {Name} started");
            Document dest = TransformImpl(source);
            _output.WriteInfoLine($"Execution of {Name} finished");
            return dest;
        }

        private Document TransformImpl(Document source)
        {
            DocumentEditor documentEditor = DocumentEditor.CreateAsync(source).Result;
            SyntaxNode? sourceRoot = source.GetSyntaxRootAsync().Result;
            if (sourceRoot == null)
                throw new InvalidOperationException("Bad source document (without syntax tree)");
            ObjectCreationExpressionSyntax[] objectCreationExpressions = sourceRoot.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
                .ToArray();
            foreach (ObjectCreationExpressionSyntax objectCreationExpr in objectCreationExpressions)
                ProcessInitializerExpression(documentEditor, objectCreationExpr);
            Document destDocument = documentEditor.GetChangedDocument();
            return destDocument;
        }

        private void ProcessInitializerExpression(DocumentEditor documentEditor, ObjectCreationExpressionSyntax objectCreationExpr)
        {
            FileLinePositionSpan location = objectCreationExpr.SyntaxTree.GetLineSpan(objectCreationExpr.Span);
            switch (objectCreationExpr.Initializer)
            {
                case null:
                    return;
                case var initializer when initializer.Kind() == SyntaxKind.ObjectInitializerExpression:
                {
                    switch (objectCreationExpr.Parent)
                    {
                        case null:
                            throw new InvalidOperationException($"Bad object creation expression: parent is null (location is {location})");
                        case InitializerExpressionSyntax:
                        case AssignmentExpressionSyntax {Parent: InitializerExpressionSyntax}:
                            break;
                        case AssignmentExpressionSyntax assignmentExpr:
                            ProcessAssignmentExpression(documentEditor, objectCreationExpr, assignmentExpr);
                            break;
                        case EqualsValueClauseSyntax equalsValueClause:
                            ProcessEqualsValueClause(documentEditor, objectCreationExpr, equalsValueClause);
                            break;
                        default:
                            String message = $"Unsupported kind of parent of object creation expression: {objectCreationExpr.Parent.Kind()} (location is {location})";
                            throw new InvalidOperationException(message);
                    }
                    return;
                }
                // TODO (std_string) : think about processing of other type of initializer expressions
                default:
                    return;
            }
        }

        private void ProcessAssignmentExpression(DocumentEditor documentEditor,
                                                 ObjectCreationExpressionSyntax objectCreationExpr,
                                                 AssignmentExpressionSyntax assignmentExpr)
        {
            FileLinePositionSpan location = assignmentExpr.SyntaxTree.GetLineSpan(assignmentExpr.Span);
            switch (assignmentExpr.Parent)
            {
                case ExpressionStatementSyntax expressionStatement:
                    SyntaxTrivia leadingSpaceTrivia = TriviaHelper.GetLeadingSpaceTrivia(expressionStatement);
                    SyntaxTrivia eolTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(expressionStatement);
                    SyntaxTriviaList leadingTrivia = PrepareLeadingTrivia(assignmentExpr.GetLeadingTrivia(), leadingSpaceTrivia, eolTrivia);
                    SyntaxTriviaList trailingTrivia = PrepareTrailingTriviaForObjectCreation(objectCreationExpr, eolTrivia);
                    ArgumentListSyntax argList = SyntaxFactory.ArgumentList(objectCreationExpr.ArgumentList?.Arguments ?? new SeparatedSyntaxList<ArgumentSyntax>());
                    ObjectCreationExpressionSyntax newObjectCreationExpr = SyntaxFactory.ObjectCreationExpression(objectCreationExpr.Type, argList, null);
                    ExpressionSyntax assignmentExprLeft = assignmentExpr.Left;
                    IList<StatementSyntax> newStatements = new List<StatementSyntax>();
                    ProcessAssignmentExpression(assignmentExprLeft, newObjectCreationExpr, leadingTrivia, trailingTrivia, newStatements);
                    CollectObjectInitializerExpressions(assignmentExprLeft, objectCreationExpr.Initializer!, leadingSpaceTrivia, eolTrivia, newStatements);
                    documentEditor.ReplaceStatement(expressionStatement, newStatements);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected parent for assignment expression (location is {location})");
            }
        }

        private void ProcessAssignmentExpression(ExpressionSyntax assignmentLeftPart,
                                                 ExpressionSyntax assignmentRightPart,
                                                 SyntaxTriviaList leadingTrivia,
                                                 SyntaxTriviaList trailingTrivia,
                                                 IList<StatementSyntax> statementDest)
        {
            AssignmentExpressionSyntax newAssignmentExpr = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                assignmentLeftPart,
                assignmentRightPart);
            ExpressionStatementSyntax newExprStatement = SyntaxFactory.ExpressionStatement(newAssignmentExpr)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                .NormalizeWhitespace()
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(trailingTrivia);
            statementDest.Add(newExprStatement);
        }

        private void ProcessEqualsValueClause(DocumentEditor documentEditor,
                                              ObjectCreationExpressionSyntax objectCreationExpr,
                                              EqualsValueClauseSyntax equalsValueClause)
        {
            FileLinePositionSpan location = equalsValueClause.SyntaxTree.GetLineSpan(equalsValueClause.Span);
            switch (equalsValueClause.Parent)
            {
                case VariableDeclaratorSyntax {Parent: VariableDeclarationSyntax {Parent: LocalDeclarationStatementSyntax localDeclarationStatement}}:
                    VariableDeclarationSyntax variableDeclaration = localDeclarationStatement.Declaration;
                    if (variableDeclaration.Variables.Count > 1)
                        throw new NotSupportedException("More that one variable declarations is not supported now");
                    VariableDeclaratorSyntax variableDeclarator = variableDeclaration.Variables.First();
                    SyntaxToken identifier = variableDeclarator.Identifier;
                    IdentifierNameSyntax identifierName = SyntaxFactory.IdentifierName(identifier);
                    SyntaxTrivia leadingSpaceTrivia = TriviaHelper.GetLeadingSpaceTrivia(localDeclarationStatement);
                    SyntaxTrivia eolTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(localDeclarationStatement);
                    SyntaxTriviaList leadingTrivia = PrepareLeadingTrivia(variableDeclaration.GetLeadingTrivia(), leadingSpaceTrivia, eolTrivia);
                    SyntaxTriviaList trailingTrivia = PrepareTrailingTriviaForObjectCreation(objectCreationExpr, eolTrivia);
                    ArgumentListSyntax argList = SyntaxFactory.ArgumentList(objectCreationExpr.ArgumentList?.Arguments ?? new SeparatedSyntaxList<ArgumentSyntax>());
                    ObjectCreationExpressionSyntax newObjectCreationExpr = SyntaxFactory.ObjectCreationExpression(objectCreationExpr.Type, argList, null);
                    EqualsValueClauseSyntax newEqualsValueClause = SyntaxFactory.EqualsValueClause(newObjectCreationExpr);
                    VariableDeclaratorSyntax newVariableDeclarator = SyntaxFactory.VariableDeclarator(identifier, null, newEqualsValueClause);
                    SeparatedSyntaxList<VariableDeclaratorSyntax> newVariableDeclarators = SyntaxFactory.SeparatedList(new[] {newVariableDeclarator});
                    VariableDeclarationSyntax newVariableDeclaration = SyntaxFactory.VariableDeclaration(variableDeclaration.Type, newVariableDeclarators);
                    LocalDeclarationStatementSyntax newLocalDeclarationStatement = SyntaxFactory.LocalDeclarationStatement(newVariableDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        .NormalizeWhitespace()
                        .WithLeadingTrivia(leadingTrivia)
                        .WithTrailingTrivia(trailingTrivia);
                    IList<StatementSyntax> newStatements = new List<StatementSyntax>{newLocalDeclarationStatement};
                    CollectObjectInitializerExpressions(identifierName, objectCreationExpr.Initializer!, leadingSpaceTrivia, eolTrivia, newStatements);
                    documentEditor.ReplaceStatement(localDeclarationStatement, newStatements);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected parent for equal value clause (location is {location})");
            }
        }

        private void CollectObjectInitializerExpressions(ExpressionSyntax baseLeftAssignment,
                                                         InitializerExpressionSyntax initializerExpression,
                                                         SyntaxTrivia leadingSpaceTrivia,
                                                         SyntaxTrivia eolTrivia,
                                                         IList<StatementSyntax> newStatements)
        {
            foreach (ExpressionSyntax expression in initializerExpression.Expressions)
            {
                FileLinePositionSpan location = expression.SyntaxTree.GetLineSpan(expression.Span);
                SyntaxTriviaList leadingTrivia = PrepareLeadingTrivia(expression.GetLeadingTrivia(), leadingSpaceTrivia, eolTrivia);
                switch (expression)
                {
                    case AssignmentExpressionSyntax {Left: SimpleNameSyntax name, Right: ObjectCreationExpressionSyntax objectCreationExpr}
                        when objectCreationExpr.Initializer.IsKind(SyntaxKind.ObjectInitializerExpression):
                    {
                        SyntaxTriviaList trailingTrivia = PrepareTrailingTriviaForObjectCreation(objectCreationExpr, eolTrivia);
                        SimpleNameSyntax memberName = SyntaxFactory.IdentifierName(name.Identifier.Text);
                        MemberAccessExpressionSyntax leftAssignment = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, baseLeftAssignment, memberName);
                        ArgumentListSyntax arguments = SyntaxFactory.ArgumentList(objectCreationExpr.ArgumentList?.Arguments ?? new SeparatedSyntaxList<ArgumentSyntax>());
                        ObjectCreationExpressionSyntax rightAssignment = SyntaxFactory.ObjectCreationExpression(objectCreationExpr.Type, arguments, null);
                        ProcessAssignmentExpression(leftAssignment, rightAssignment, leadingTrivia, trailingTrivia, newStatements);
                        if (objectCreationExpr.Initializer != null)
                            CollectObjectInitializerExpressions(leftAssignment, objectCreationExpr.Initializer, leadingSpaceTrivia, eolTrivia, newStatements);
                        break;
                    }
                    case AssignmentExpressionSyntax {Left: SimpleNameSyntax name, Right: var rightAssignmentExpr}:
                    {
                        SyntaxTriviaList trailingTrivia = PrepareTrailingTriviaForInitializerExpr(expression, eolTrivia);
                        SimpleNameSyntax memberName = SyntaxFactory.IdentifierName(name.Identifier.Text);
                        MemberAccessExpressionSyntax leftAssignment = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, baseLeftAssignment, memberName);
                        ExpressionSyntax rightAssignment = rightAssignmentExpr.WithLeadingTrivia().WithTrailingTrivia();
                        ProcessAssignmentExpression(leftAssignment, rightAssignment, leadingTrivia, trailingTrivia, newStatements);
                        break;
                    }
                    default:
                        throw new InvalidOperationException($"Unexpected syntax node rather than AssignmentExpressionSyntax (location is {location})");
                }
            }
        }

        private SyntaxTriviaList PrepareLeadingTrivia(SyntaxTriviaList sourceTrivia, SyntaxTrivia leadingSpaceTrivia, SyntaxTrivia eolTrivia)
        {
            IList<SyntaxTrivia> comments = TriviaHelper.ConstructSingleLineCommentsTrivia(sourceTrivia, leadingSpaceTrivia, eolTrivia);
            comments.Add(leadingSpaceTrivia);
            return new SyntaxTriviaList(comments);
        }

        private SyntaxTriviaList PrepareTrailingTriviaForObjectCreation(ObjectCreationExpressionSyntax objectCreationExpr, SyntaxTrivia eolTrivia)
        {
            SyntaxTriviaList trailingTrivia = objectCreationExpr.ArgumentList?.GetTrailingTrivia() ?? new SyntaxTriviaList();
            IList<SyntaxTrivia> comments = TriviaHelper.ConstructSingleLineCommentsTrivia(trailingTrivia, 1, eolTrivia);
            return comments.IsEmpty() ? new SyntaxTriviaList(eolTrivia) : new SyntaxTriviaList(comments);
        }

        private SyntaxTriviaList PrepareTrailingTriviaForInitializerExpr(ExpressionSyntax expression, SyntaxTrivia eolTrivia)
        {
            SyntaxToken lastToken = expression.GetLastToken();
            SyntaxToken nextToken = lastToken.GetNextToken();
            SyntaxTriviaList trailingTrivia = nextToken switch
            {
                _ when nextToken.IsKind(SyntaxKind.CommaToken) => nextToken.TrailingTrivia,
                _ => lastToken.TrailingTrivia
            };
            IList<SyntaxTrivia> comments = TriviaHelper.ConstructSingleLineCommentsTrivia(trailingTrivia, 1, eolTrivia);
            return comments.IsEmpty() ? new SyntaxTriviaList(eolTrivia) : new SyntaxTriviaList(comments);
        }

        private readonly IOutput _output;
        private readonly TransformerState _transformerState;
    }
}
