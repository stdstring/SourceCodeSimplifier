using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using SourceCodeSimplifierApp.Config;
using SourceCodeSimplifierApp.Output;
using SourceCodeSimplifierApp.Utils;

namespace SourceCodeSimplifierApp.Transformers
{
    internal class ObjectInitializerTransformer : ITransformer
    {
        public const String Name = "SourceCodeSimplifierApp.Transformers.ObjectInitializerTransformer";

        public ObjectInitializerTransformer(IOutput output, TransformerState transformerState)
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
            switch (objectCreationExpr.Initializer)
            {
                case null:
                    return;
                case var initializer when initializer.Kind() == SyntaxKind.ObjectInitializerExpression:
                {
                    switch (objectCreationExpr.Parent)
                    {
                        case null:
                            throw new InvalidOperationException("Bad object creation expression: parent is null");
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
                            throw new InvalidOperationException($"Unsupported kind of parent of object creation expression: {objectCreationExpr.Parent.Kind()}");
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
            switch (assignmentExpr.Parent)
            {
                case ExpressionStatementSyntax expressionStatement:
                    SyntaxTrivia leadingTrivia = TriviaHelper.GetLeadingSpaceTrivia(expressionStatement);
                    SyntaxTrivia trailingTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(expressionStatement);
                    ArgumentListSyntax argList = objectCreationExpr.ArgumentList ?? SyntaxFactory.ArgumentList();
                    ObjectCreationExpressionSyntax newObjectCreationExpr = SyntaxFactory.ObjectCreationExpression(objectCreationExpr.Type, argList, null);
                    ExpressionSyntax assignmentExprLeft = assignmentExpr.Left;
                    AssignmentExpressionSyntax newAssignmentExpr = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        assignmentExprLeft,
                        newObjectCreationExpr);
                    ExpressionStatementSyntax newExprStatement = SyntaxFactory.ExpressionStatement(newAssignmentExpr)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        .NormalizeWhitespace()
                        .WithLeadingTrivia(leadingTrivia)
                        .WithTrailingTrivia(trailingTrivia);
                    IList<StatementSyntax> newStatements = new List<StatementSyntax>{newExprStatement};
                    CollectObjectInitializerExpressions(assignmentExprLeft, objectCreationExpr.Initializer!, leadingTrivia, trailingTrivia, newStatements);
                    documentEditor.ReplaceStatement(expressionStatement, newStatements);
                    break;
                default:
                    throw new InvalidOperationException("Unexpected parent for assignment expression");
            }
        }

        private void ProcessEqualsValueClause(DocumentEditor documentEditor,
                                              ObjectCreationExpressionSyntax objectCreationExpr,
                                              EqualsValueClauseSyntax equalsValueClause)
        {
            switch (equalsValueClause.Parent)
            {
                case VariableDeclaratorSyntax {Parent: VariableDeclarationSyntax {Parent: LocalDeclarationStatementSyntax localDeclarationStatement}}:
                    VariableDeclarationSyntax variableDeclaration = localDeclarationStatement.Declaration;
                    if (variableDeclaration.Variables.Count > 1)
                        throw new NotSupportedException("More that one variable declarations is not supported now");
                    VariableDeclaratorSyntax variableDeclarator = variableDeclaration.Variables.First();
                    SyntaxToken identifier = variableDeclarator.Identifier;
                    IdentifierNameSyntax identifierName = SyntaxFactory.IdentifierName(identifier);
                    SyntaxTrivia leadingTrivia = TriviaHelper.GetLeadingSpaceTrivia(localDeclarationStatement);
                    SyntaxTrivia trailingTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(localDeclarationStatement);
                    ArgumentListSyntax argList = objectCreationExpr.ArgumentList ?? SyntaxFactory.ArgumentList();
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
                    CollectObjectInitializerExpressions(identifierName, objectCreationExpr.Initializer!, leadingTrivia, trailingTrivia, newStatements);
                    documentEditor.ReplaceStatement(localDeclarationStatement, newStatements);
                    break;
                default:
                    throw new InvalidOperationException("Unexpected parent for equal value clause");
            }
        }

        private void CollectObjectInitializerExpressions(ExpressionSyntax baseLeftAssignment,
                                                         InitializerExpressionSyntax initializerExpression,
                                                         SyntaxTrivia leadingTrivia,
                                                         SyntaxTrivia trailingTrivia,
                                                         IList<StatementSyntax> newStatements)
        {
            foreach (ExpressionSyntax expression in initializerExpression.Expressions)
            {
                switch (expression)
                {
                    case AssignmentExpressionSyntax {Left: SimpleNameSyntax name, Right: ObjectCreationExpressionSyntax objectCreationExpr}:
                    {
                        MemberAccessExpressionSyntax leftAssignment = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, baseLeftAssignment, name);
                        ArgumentListSyntax arguments = objectCreationExpr.ArgumentList ?? SyntaxFactory.ArgumentList();
                        ObjectCreationExpressionSyntax rightAssignment = SyntaxFactory.ObjectCreationExpression(objectCreationExpr.Type, arguments, null);
                        AssignmentExpressionSyntax resultAssignment = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, leftAssignment, rightAssignment);
                        ExpressionStatementSyntax resultStatement = SyntaxFactory.ExpressionStatement(resultAssignment)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            .NormalizeWhitespace()
                            .WithLeadingTrivia(leadingTrivia)
                            .WithTrailingTrivia(trailingTrivia);
                        newStatements.Add(resultStatement);
                        if (objectCreationExpr.Initializer != null)
                            CollectObjectInitializerExpressions(leftAssignment, objectCreationExpr.Initializer, leadingTrivia, trailingTrivia, newStatements);
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
                        newStatements.Add(resultStatement);
                            break;
                    }
                    default:
                        throw new InvalidOperationException("Unexpected syntax node rather than AssignmentExpressionSyntax");
                }
            }
        }

        private readonly IOutput _output;
        private readonly TransformerState _transformerState;
    }
}
