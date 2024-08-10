using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using SourceCodeSimplifierApp.Config;
using SourceCodeSimplifierApp.Output;
using SourceCodeSimplifierApp.Utils;
using SourceCodeSimplifierApp.Variables;

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
            String filename = source.FilePath ?? source.Name;
            DocumentEditor documentEditor = DocumentEditor.CreateAsync(source).Result;
            SyntaxNode? sourceRoot = source.GetSyntaxRootAsync().Result;
            if (sourceRoot == null)
                throw new InvalidOperationException("Bad source document (without syntax tree)");
            ObjectCreationExpressionSyntax[] objectCreationExpressions = sourceRoot.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
                .ToArray();
            StatementSyntax[] parentStatements = objectCreationExpressions
                .Select(expression => expression.GetParentStatement())
                .Distinct()
                .ToArray();
            VariableManager variableManager = new VariableManager();
            SemanticModel model = documentEditor.SemanticModel;
            foreach (StatementSyntax parentStatement in parentStatements)
            {
                IList<StatementSyntax> beforeStatements = new List<StatementSyntax>();
                IList<StatementSyntax> afterStatements = new List<StatementSyntax>();
                ObjectInitializerExprSyntaxRewriter rewriter = new ObjectInitializerExprSyntaxRewriter(model, variableManager, beforeStatements, afterStatements, _output, filename);
                StatementSyntax result = rewriter.Visit(parentStatement).MustCast<SyntaxNode, StatementSyntax>();
                IList<StatementSyntax> newStatements = new List<StatementSyntax>();
                newStatements.AddRange(beforeStatements);
                newStatements.Add(result);
                newStatements.AddRange(afterStatements);
                documentEditor.ReplaceStatement(parentStatement, newStatements);
            }
            Document destDocument = documentEditor.GetChangedDocument();
            return destDocument;
        }

        private readonly IOutput _output;
        private readonly TransformerState _transformerState;
    }

    internal class ObjectInitializerExprSyntaxRewriter : CSharpSyntaxRewriter
    {
        public ObjectInitializerExprSyntaxRewriter(SemanticModel model,
                                                   VariableManager variableManager,
                                                   IList<StatementSyntax> beforeStatements,
                                                   IList<StatementSyntax> afterStatements,
                                                   IOutput output,
                                                   String filename)
        {
            _model = model;
            _variableManager = variableManager;
            _beforeStatements = beforeStatements;
            _afterStatements = afterStatements;
            _output = output;
            _filename = filename;
        }

        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            CheckTrailingTrivia(node);
            SyntaxNode? expression = base.VisitObjectCreationExpression(node);
            switch (expression)
            {
                case null:
                    throw new InvalidOperationException("Bad result of ObjectCreationExpression transformation");
                case ObjectCreationExpressionSyntax destExpression:
                {
                    switch (node)
                    {
                        case {Initializer: null}:
                            return destExpression;
                        case {Initializer: var initializer} when initializer.IsKind(SyntaxKind.CollectionInitializerExpression):
                            return destExpression;
                        case {Parent: ArgumentSyntax argument}:
                            return ProcessObjectCreationExpressionInArg(node, destExpression, argument);
                        case {Parent: ReturnStatementSyntax}:
                            return ProcessObjectCreationExpressionWithVar(node, destExpression, "returnValue");
                        case {Parent: InitializerExpressionSyntax}:
                            return ProcessObjectCreationExpressionWithVar(node, destExpression, "initValue");
                        case {Parent: AssignmentExpressionSyntax{Parent: InitializerExpressionSyntax}}:
                            return destExpression;
                        case {Parent: AssignmentExpressionSyntax{Parent: ExpressionStatementSyntax statement, Left: var assignmentLeft}}:
                            return ProcessObjectCreationExpressionForAssignment(destExpression, statement, assignmentLeft);
                        case {Parent: EqualsValueClauseSyntax{Parent: VariableDeclaratorSyntax{Parent: VariableDeclarationSyntax{Parent: LocalDeclarationStatementSyntax statement}}}}:
                            return ProcessObjectCreationExpressionForLocalDecl(destExpression, statement);
                        default:
                            return destExpression;
                    }
                }
                default:
                    return expression;
            }
        }

        private void CheckTrailingTrivia(ObjectCreationExpressionSyntax node)
        {
            if (node.Initializer == null)
                return;
            SyntaxTrivia? trailingTrivia = ObjectInitializerExprTrivia.ExtractTrailingTrivia(node);
            if (trailingTrivia == null)
                return;
            FileLinePositionSpan location = node.SyntaxTree.GetLineSpan(node.Span);
            _output.WriteWarningLine(_filename, location.StartLinePosition.Line, $"Unprocessed (lost) trailing comment: \"{trailingTrivia}\"");
        }

        private SyntaxNode ProcessObjectCreationExpressionInArg(ObjectCreationExpressionSyntax source,
                                                                ObjectCreationExpressionSyntax current,
                                                                ArgumentSyntax argument)
        {
            IOperation? operationInfo = _model.GetOperation(argument);
            switch (operationInfo)
            {
                case null:
                    throw new InvalidOperationException("Bad object initializer expression: absence type info");
                case IArgumentOperation {Parameter: null}:
                    throw new InvalidOperationException("Bad object initializer expression: absence parameter info");
                case IArgumentOperation {Parameter: var parameterInfo}:
                return ProcessObjectCreationExpressionWithVar(source, current, parameterInfo.Name);
                default:
                    throw new InvalidOperationException("Bad object initializer expression: unknown operation info");
            }
        }

        private SyntaxNode ProcessObjectCreationExpressionWithVar(ObjectCreationExpressionSyntax source,
                                                                  ObjectCreationExpressionSyntax current,
                                                                  String prefixName)
        {
            String variableName = _variableManager.GenerateVariableName(source, prefixName);
            String typeName = current.Type.ToString();
            StatementSyntax parentStatement = source.GetParentStatement();
            SyntaxTrivia leadingSpaceTrivia = TriviaHelper.GetLeadingSpaceTrivia(parentStatement);
            SyntaxTrivia eolTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(parentStatement);
            IList<StatementSyntax> dest = CollectForLocalDeclarationStatement(variableName, typeName, current, leadingSpaceTrivia, eolTrivia);
            _beforeStatements.AddRange(dest);
            return SyntaxFactory.IdentifierName(variableName);
        }

        private SyntaxNode ProcessObjectCreationExpressionForAssignment(ObjectCreationExpressionSyntax current,
                                                                        ExpressionStatementSyntax parentStatement,
                                                                        ExpressionSyntax assignmentLeft)
        {
            SyntaxTrivia leadingSpaceTrivia = TriviaHelper.GetLeadingSpaceTrivia(parentStatement);
            SyntaxTrivia eolTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(parentStatement);
            IList<StatementSyntax> dest = new List<StatementSyntax>();
            CollectObjectInitializerExpressions(assignmentLeft, current.Initializer!, leadingSpaceTrivia, eolTrivia, dest);
            _afterStatements.AddRange(dest);
            ArgumentListSyntax argList = SyntaxFactory.ArgumentList(current.ArgumentList?.Arguments ?? new SeparatedSyntaxList<ArgumentSyntax>());
            return SyntaxFactory.ObjectCreationExpression(current.Type, argList, null).NormalizeWhitespace();
        }

        private SyntaxNode ProcessObjectCreationExpressionForLocalDecl(ObjectCreationExpressionSyntax current,
                                                                       LocalDeclarationStatementSyntax parentStatement)
        {
            VariableDeclarationSyntax variableDeclaration = parentStatement.Declaration;
            if (variableDeclaration.Variables.Count > 1)
                throw new NotSupportedException("More than one variable declarations is not supported now");
            VariableDeclaratorSyntax variableDeclarator = variableDeclaration.Variables.First();
            IdentifierNameSyntax identifier = SyntaxFactory.IdentifierName(variableDeclarator.Identifier);
            SyntaxTrivia leadingSpaceTrivia = TriviaHelper.GetLeadingSpaceTrivia(parentStatement);
            SyntaxTrivia eolTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(parentStatement);
            IList<StatementSyntax> dest = new List<StatementSyntax>();
            CollectObjectInitializerExpressions(identifier, current.Initializer!, leadingSpaceTrivia, eolTrivia, dest);
            _afterStatements.AddRange(dest);
            ArgumentListSyntax argList = SyntaxFactory.ArgumentList(current.ArgumentList?.Arguments ?? new SeparatedSyntaxList<ArgumentSyntax>());
            return SyntaxFactory.ObjectCreationExpression(current.Type, argList, null)
                .NormalizeWhitespace();
        }

        public IList<StatementSyntax> CollectForLocalDeclarationStatement(String identifierName,
                                                                          String typeName,
                                                                          ObjectCreationExpressionSyntax objectCreationExpr,
                                                                          SyntaxTrivia leadingSpaceTrivia,
                                                                          SyntaxTrivia eolTrivia)
        {
            SyntaxTriviaList leadingTrivia = new SyntaxTriviaList(leadingSpaceTrivia);
            SyntaxTriviaList trailingTrivia = new SyntaxTriviaList(eolTrivia);
            return CollectForLocalDeclarationStatement(identifierName, typeName, objectCreationExpr, leadingSpaceTrivia, eolTrivia, leadingTrivia, trailingTrivia);
        }

        public IList<StatementSyntax> CollectForLocalDeclarationStatement(String identifierName,
                                                                          String typeName,
                                                                          ObjectCreationExpressionSyntax objectCreationExpr,
                                                                          SyntaxTrivia leadingSpaceTrivia,
                                                                          SyntaxTrivia eolTrivia,
                                                                          SyntaxTriviaList leadingTrivia,
                                                                          SyntaxTriviaList trailingTrivia)
        {
            IdentifierNameSyntax identifier = SyntaxFactory.IdentifierName(identifierName);
            ArgumentListSyntax argList = SyntaxFactory.ArgumentList(objectCreationExpr.ArgumentList?.Arguments ?? new SeparatedSyntaxList<ArgumentSyntax>());
            ObjectCreationExpressionSyntax newObjectCreationExpr = SyntaxFactory.ObjectCreationExpression(objectCreationExpr.Type, argList, null)
                .NormalizeWhitespace();
            StatementSyntax newLocalDeclarationStatement = SyntaxFactory.ParseStatement($"{typeName} {identifierName} = {newObjectCreationExpr};")
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(trailingTrivia);
            IList<StatementSyntax> objectInitializerExprStatements = new List<StatementSyntax> { newLocalDeclarationStatement };
            CollectObjectInitializerExpressions(identifier, objectCreationExpr.Initializer!, leadingSpaceTrivia, eolTrivia, objectInitializerExprStatements);
            return objectInitializerExprStatements;
        }

        public void CollectObjectInitializerExpressions(ExpressionSyntax baseLeftAssignment,
                                                        InitializerExpressionSyntax initializerExpression,
                                                        SyntaxTrivia leadingSpaceTrivia,
                                                        SyntaxTrivia eolTrivia,
                                                        IList<StatementSyntax> newStatements)
        {
            baseLeftAssignment = baseLeftAssignment.WithLeadingTrivia().WithTrailingTrivia();
            foreach (ExpressionSyntax expression in initializerExpression.Expressions)
            {
                FileLinePositionSpan location = expression.SyntaxTree.GetLineSpan(expression.Span);
                SyntaxTriviaList leadingTrivia = TriviaHelper.ConstructLeadingTrivia(expression.GetLeadingTrivia(), leadingSpaceTrivia, eolTrivia);
                switch (expression)
                {
                    case AssignmentExpressionSyntax {Left: SimpleNameSyntax name, Right: ObjectCreationExpressionSyntax objectCreationExpr}
                        when objectCreationExpr.Initializer.IsKind(SyntaxKind.ObjectInitializerExpression):
                    {
                        SyntaxTriviaList trailingTrivia = ObjectInitializerExprTrivia.ConstructTrailingTrivia(objectCreationExpr, eolTrivia);
                        SimpleNameSyntax memberName = SyntaxFactory.IdentifierName(name.Identifier.Text);
                        MemberAccessExpressionSyntax leftAssignment = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, baseLeftAssignment, memberName);
                        ArgumentListSyntax arguments = SyntaxFactory.ArgumentList(objectCreationExpr.ArgumentList?.Arguments ?? new SeparatedSyntaxList<ArgumentSyntax>());
                        ObjectCreationExpressionSyntax rightAssignment = SyntaxFactory.ObjectCreationExpression(objectCreationExpr.Type, arguments, null)
                            .NormalizeWhitespace();
                        ProcessAssignmentExpression(leftAssignment, rightAssignment, leadingTrivia, trailingTrivia, newStatements);
                        if (objectCreationExpr.Initializer != null)
                            CollectObjectInitializerExpressions(leftAssignment, objectCreationExpr.Initializer, leadingSpaceTrivia, eolTrivia, newStatements);
                        break;
                    }
                    case AssignmentExpressionSyntax {Left: SimpleNameSyntax name, Right: InitializerExpressionSyntax innerInitializerExpression}:
                    {
                        SimpleNameSyntax memberName = SyntaxFactory.IdentifierName(name.Identifier.Text);
                        MemberAccessExpressionSyntax leftAssignment = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, baseLeftAssignment, memberName);
                        CollectObjectInitializerExpressions(leftAssignment, innerInitializerExpression, leadingSpaceTrivia, eolTrivia, newStatements);
                        break;
                    }
                    case AssignmentExpressionSyntax {Left: SimpleNameSyntax name, Right: var rightAssignmentExpr}:
                    {
                        SyntaxTriviaList trailingTrivia = ObjectInitializerExprTrivia.ConstructTrailingTrivia(expression, eolTrivia);
                        ProcessAssignmentExpression($"{baseLeftAssignment}.{name}", rightAssignmentExpr, leadingTrivia, trailingTrivia, newStatements);
                        break;
                    }
                    default:
                        throw new InvalidOperationException($"Unexpected syntax node rather than AssignmentExpressionSyntax (location is {location})");
                }
            }
        }

        private void ProcessAssignmentExpression(ExpressionSyntax assignmentLeftPart,
                                                 ExpressionSyntax assignmentRightPart,
                                                 SyntaxTriviaList leadingTrivia,
                                                 SyntaxTriviaList trailingTrivia,
                                                 IList<StatementSyntax> statementDest)
        {
            ProcessAssignmentExpression(assignmentLeftPart.ToString(), assignmentRightPart, leadingTrivia, trailingTrivia, statementDest);
        }

        private void ProcessAssignmentExpression(String assignmentLeftPart,
                                                 ExpressionSyntax assignmentRightPart,
                                                 SyntaxTriviaList leadingTrivia,
                                                 SyntaxTriviaList trailingTrivia,
                                                 IList<StatementSyntax> statementDest)
        {
            StatementSyntax statement = SyntaxFactory.ParseStatement($"{assignmentLeftPart} = {assignmentRightPart};")
                .WithAdditionalAnnotations(Formatter.Annotation)
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(trailingTrivia);
            statementDest.Add(statement);
        }

        private readonly SemanticModel _model;
        private readonly IList<StatementSyntax> _beforeStatements;
        private readonly IList<StatementSyntax> _afterStatements;
        private readonly VariableManager _variableManager;
        private readonly IOutput _output;
        private readonly String _filename;
    }

    internal static class ObjectInitializerExprTrivia
    {
        public static SyntaxTrivia? ExtractTrailingTrivia(ObjectCreationExpressionSyntax objectCreationExpr)
        {
            SyntaxTriviaList trailingTrivia = objectCreationExpr.ArgumentList?.GetTrailingTrivia() ?? new SyntaxTriviaList();
            return trailingTrivia.Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)).ToArray() switch
            {
                [] => null,
                [var commentTrivia] => commentTrivia,
                _ => throw new InvalidOperationException("Bad (several) trailing trivia")
            };
        }

        public static SyntaxTriviaList ConstructTrailingTrivia(ObjectCreationExpressionSyntax objectCreationExpr, SyntaxTrivia eolTrivia)
        {
            SyntaxTriviaList trailingTrivia = objectCreationExpr.ArgumentList?.GetTrailingTrivia() ?? new SyntaxTriviaList();
            IList<SyntaxTrivia> comments = TriviaHelper.ConstructSingleLineCommentsTrivia(trailingTrivia, 1, eolTrivia);
            return comments.IsEmpty() ? new SyntaxTriviaList(eolTrivia) : new SyntaxTriviaList(comments);
        }

        public static SyntaxTriviaList ConstructTrailingTrivia(ExpressionSyntax expression, SyntaxTrivia eolTrivia)
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
    }
}
