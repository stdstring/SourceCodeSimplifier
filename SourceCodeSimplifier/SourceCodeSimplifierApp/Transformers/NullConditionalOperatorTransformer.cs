using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using SourceCodeSimplifierApp.Config;
using SourceCodeSimplifierApp.Output;
using SourceCodeSimplifierApp.Utils;
using SourceCodeSimplifierApp.Variables;

namespace SourceCodeSimplifierApp.Transformers
{
    internal class NullConditionalOperatorTransformer : ITransformer
    {
        public const String Name = "SourceCodeSimplifierApp.Transformers.NullConditionalOperatorTransformer";

        public NullConditionalOperatorTransformer(IOutput output, TransformerState transformerState)
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
            SyntaxNode sourceRoot = source.GetSyntaxRootAsync().Result.Must();
            ConditionalAccessExpressionSyntax[] conditionalAccessExpressions = sourceRoot
                .DescendantNodes()
                .OfType<ConditionalAccessExpressionSyntax>()
                .ToArray();
            StatementSyntax[] parentStatements = conditionalAccessExpressions
                .Select(expression => expression.GetParentStatement())
                .Distinct()
                .ToArray();
            VariableManager variableManager = new VariableManager();
            SemanticModel model = documentEditor.SemanticModel;
            foreach (StatementSyntax parentStatement in parentStatements)
            {
                NullConditionalOperatorRewriter rewriter = new NullConditionalOperatorRewriter(model, variableManager);
                IList<StatementSyntax> destStatements = rewriter.Process(parentStatement);
                documentEditor.ReplaceStatement(parentStatement, destStatements);
            }
            Document destDocument = documentEditor.GetChangedDocument();
            return destDocument;
        }

        private readonly IOutput _output;
        private readonly TransformerState _transformerState;
    }

    internal class NullConditionalOperatorRewriter : CSharpSyntaxRewriter
    {
        public NullConditionalOperatorRewriter(SemanticModel model, VariableManager variableManager)
        {
            _model = model;
            _variableManager = variableManager;
            _destStatements = new List<String>();
        }

        public IList<StatementSyntax> Process(StatementSyntax sourceStatement)
        {
            _destStatements = new List<String>();
            _includeTransformedSource = true;
            StatementSyntax destStatement = Visit(sourceStatement).MustCast<SyntaxNode, StatementSyntax>();
            IList<StatementSyntax> result = _destStatements.Select(statement => SyntaxFactory.ParseStatement(statement)).ToList();
            if (_includeTransformedSource)
                result.Add(destStatement);
            return result;
        }

        public override SyntaxNode VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            NullConditionalOperatorChildRewriter childRewriter = new NullConditionalOperatorChildRewriter(_model, _variableManager);
            SyntaxNode result = childRewriter.VisitConditionalAccessExpression(node);
            _destStatements.AddRange(childRewriter.Dest);
            _includeTransformedSource = node.Parent switch
            {
                AssignmentExpressionSyntax => false,
                EqualsValueClauseSyntax{Parent: VariableDeclaratorSyntax{Parent: VariableDeclarationSyntax{Parent: LocalDeclarationStatementSyntax}}} => false,
                ExpressionStatementSyntax => false,
                _ => true
            };
            return result;
        }

        public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            switch (node)
            {
                case var _ when node.IsKind(SyntaxKind.CoalesceExpression):
                    return VisitCoalesceExpression(node);
                default:
                    return base.VisitBinaryExpression(node);
            }
        }

        private SyntaxNode VisitCoalesceExpression(BinaryExpressionSyntax node)
        {
            NullConditionalOperatorChildRewriter childRewriter = new NullConditionalOperatorChildRewriter(_model, _variableManager);
            SyntaxNode result = childRewriter.VisitBinaryExpression(node);
            _destStatements.AddRange(childRewriter.Dest);
            _includeTransformedSource = node.Parent switch
            {
                AssignmentExpressionSyntax => false,
                EqualsValueClauseSyntax{Parent: VariableDeclaratorSyntax{Parent: VariableDeclarationSyntax{Parent: LocalDeclarationStatementSyntax}}} => false,
                ExpressionStatementSyntax => false,
                _ => true
            };
            return result;
        }

        private readonly SemanticModel _model;
        private readonly VariableManager _variableManager;
        private IList<String> _destStatements;
        private Boolean _includeTransformedSource = true;
    }

    internal class NullConditionalOperatorChildRewriter : CSharpSyntaxRewriter
    {
        public NullConditionalOperatorChildRewriter(SemanticModel model, VariableManager variableManager)
        {
            Dest = new List<String>();
            _model = model;
            _variableManager = variableManager;
        }

        public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (!node.IsKind(SyntaxKind.CoalesceExpression))
                return node;
            Boolean hasLeftConditionalAccessExpression = node.Left.DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().Any();
            Boolean hasRightConditionalAccessExpression = node.Right.DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().Any();
            if (hasRightConditionalAccessExpression)
                throw new InvalidOperationException("Unsupported null conditional operator in right part of coalesce expression");
            if (!hasLeftConditionalAccessExpression)
                return node;
            BinaryExpressionSyntax destExpression = base.VisitBinaryExpression(node).MustCast<SyntaxNode, BinaryExpressionSyntax>();
            switch (node.Parent)
            {
                case null:
                    throw new InvalidOperationException("Bad syntax tree: BinaryExpressionSyntax node without parent");
                case AssignmentExpressionSyntax assignmentExpression:
                {
                    StatementSyntax statement = assignmentExpression.Parent.MustCast<SyntaxNode, StatementSyntax>();
                    ProcessCoalescePart(assignmentExpression.Left, destExpression.Right, statement);
                    return destExpression;
                }
                case EqualsValueClauseSyntax {Parent: VariableDeclaratorSyntax {Parent: VariableDeclarationSyntax {Parent: LocalDeclarationStatementSyntax statement}}}:
                    ProcessCoalescePartForLocalDeclarationStatement(statement, destExpression);
                    return destExpression;
                default:
                    IdentifierNameSyntax identifier = destExpression.Left.MustCast<ExpressionSyntax, IdentifierNameSyntax>();
                    ProcessCoalescePart(identifier, destExpression.Right, node.FindParentStatement().Must());
                    return identifier;
            }
        }

        public override SyntaxNode VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            ConditionalAccessExpressionSyntax destExpression = base.VisitConditionalAccessExpression(node).MustCast<SyntaxNode, ConditionalAccessExpressionSyntax>();
            switch (node.Parent)
            {
                case BinaryExpressionSyntax{Left: var leftExpr, Parent: var parent} binaryExpr
                    when binaryExpr.IsKind(SyntaxKind.CoalesceExpression) && leftExpr == node:
                    return ProcessConditionalAccessExpression(destExpression, parent);
                case BinaryExpressionSyntax{Right: var rightExpr} binaryExpr
                    when binaryExpr.IsKind(SyntaxKind.CoalesceExpression) && rightExpr == node:
                    throw new InvalidOperationException("Unsupported null conditional operator in right part of coalesce expression");
                case var parent:
                    return ProcessConditionalAccessExpression(destExpression, parent);
            }
        }

        public IList<String> Dest { get; }

        private void ProcessCoalescePartForLocalDeclarationStatement(LocalDeclarationStatementSyntax statement, BinaryExpressionSyntax expression)
        {
            VariableDeclarationSyntax variableDeclaration = statement.Declaration;
            if (variableDeclaration.Variables.Count > 1)
                throw new NotSupportedException("More than one variable declarations is not supported now");
            VariableDeclaratorSyntax variableDeclarator = variableDeclaration.Variables.First();
            IdentifierNameSyntax identifier = SyntaxFactory.IdentifierName(variableDeclarator.Identifier);
            ProcessCoalescePart(identifier, expression.Right, statement);
        }

        private void ProcessCoalescePart(ExpressionSyntax namePart, ExpressionSyntax rightPart, StatementSyntax baseStatement)
        {
            SyntaxTrivia outerLeadingTrivia = TriviaHelper.GetLeadingSpaceTrivia(baseStatement);
            SyntaxTrivia eolTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(baseStatement);
            Int32 leadingSpaceDelta = TriviaHelper.CalcLeadingSpaceDelta(baseStatement);
            SyntaxTrivia innerLeadingTrivia = TriviaHelper.ShiftRightLeadingSpaceTrivia(outerLeadingTrivia, leadingSpaceDelta);
            String coalesceStatement = $"{outerLeadingTrivia}if ({namePart} == null){eolTrivia}" +
                                       $"{outerLeadingTrivia}{{{eolTrivia}" +
                                       $"{innerLeadingTrivia}{namePart} = {rightPart};{eolTrivia}" +
                                       $"{outerLeadingTrivia}}}{eolTrivia}";
            Dest.Add(coalesceStatement);
        }

        private SyntaxNode ProcessConditionalAccessExpression(ConditionalAccessExpressionSyntax expr, SyntaxNode? parent)
        {
            switch (parent)
            {
                case null:
                    throw new InvalidOperationException("Bad syntax tree: ConditionalAccessExpression node without parent");
                case ConditionalAccessExpressionSyntax:
                    return expr;
                case AssignmentExpressionSyntax assignmentExpression:
                    ProcessAssignmentStatement(assignmentExpression, expr);
                    return expr;
                case EqualsValueClauseSyntax{Parent: VariableDeclaratorSyntax{Parent: VariableDeclarationSyntax{Parent: LocalDeclarationStatementSyntax statement}}}:
                    ProcessLocalDeclarationStatement(statement, expr);
                    return expr;
                case ExpressionStatementSyntax statement:
                    ProcessSimpleStatement(statement, expr);
                    return expr;
                case ArgumentSyntax argument:
                    return ProcessArgument(argument, expr);
                case ReturnStatementSyntax:
                    return ProcessWithLocalVariableCreation("returnExpression", expr);
                default:
                    return ProcessWithLocalVariableCreation("expression", expr);
            }
        }

        private void ProcessLocalDeclarationStatement(LocalDeclarationStatementSyntax statement, ConditionalAccessExpressionSyntax conditionalExpr)
        {
            VariableDeclarationSyntax variableDeclaration = statement.Declaration;
            if (variableDeclaration.Variables.Count > 1)
                throw new NotSupportedException("More than one variable declarations is not supported now");
            VariableDeclaratorSyntax variableDeclarator = variableDeclaration.Variables.First();
            TypeSyntax variableType = variableDeclaration.Type;
            SyntaxToken identifier = variableDeclarator.Identifier;
            SyntaxTrivia leadingSpaceTrivia = TriviaHelper.GetLeadingSpaceTrivia(statement);
            SyntaxTrivia eolTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(statement);
            SyntaxTriviaList leadingTrivia = TriviaHelper.ConstructLeadingTrivia(statement, leadingSpaceTrivia, eolTrivia);
            SyntaxTriviaList trailingTrivia = TriviaHelper.ConstructTrailingTrivia(statement, eolTrivia);
            String defaultValue = TypeDefaultValueHelper.CreateDefaultValueForType(_model, variableType);
            String destLocalDeclaration = $"{leadingTrivia}{variableType} {identifier} = {defaultValue};{trailingTrivia}";
            Dest.Add(destLocalDeclaration);
            IList<ExpressionSyntax> conditionalExprParts = SplitParts(conditionalExpr);
            AssignmentValuePartsProcessor partsProcessor = new AssignmentValuePartsProcessor(_model, _variableManager, statement, leadingSpaceTrivia, eolTrivia, identifier);
            Dest.AddRange(partsProcessor.ProcessParts(conditionalExprParts));
        }

        private void ProcessAssignmentStatement(AssignmentExpressionSyntax assignmentExpr, ConditionalAccessExpressionSyntax conditionalExpr)
        {
            StatementSyntax statement = assignmentExpr.Parent.MustCast<SyntaxNode, StatementSyntax>();
            SyntaxTrivia leadingSpaceTrivia = TriviaHelper.GetLeadingSpaceTrivia(statement);
            SyntaxTrivia eolTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(statement);
            String leftPartAssignment = $"{assignmentExpr.Left}";
            IList<ExpressionSyntax> conditionalExprParts = SplitParts(conditionalExpr);
            AssignmentValuePartsProcessor partsProcessor = new AssignmentValuePartsProcessor(_model, _variableManager, statement, leadingSpaceTrivia, eolTrivia, leftPartAssignment);
            Dest.AddRange(partsProcessor.ProcessParts(conditionalExprParts));
        }

        private void ProcessSimpleStatement(ExpressionStatementSyntax statement, ConditionalAccessExpressionSyntax conditionalExpr)
        {
            SyntaxTrivia leadingSpaceTrivia = TriviaHelper.GetLeadingSpaceTrivia(statement);
            SyntaxTrivia eolTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(statement);
            IList<ExpressionSyntax> conditionalExprParts = SplitParts(conditionalExpr);
            SimpleStatementPartsProcessor partsProcessor = new SimpleStatementPartsProcessor(_model, _variableManager, statement, leadingSpaceTrivia, eolTrivia);
            Dest.AddRange(partsProcessor.ProcessParts(conditionalExprParts));
        }

        private SyntaxNode ProcessArgument(ArgumentSyntax argument, ConditionalAccessExpressionSyntax conditionalExpr)
        {
            IOperation? operationInfo = _model.GetOperation(argument);
            switch (operationInfo)
            {
                case null:
                    throw new InvalidOperationException("Bad object initializer expression: absence type info");
                case IArgumentOperation {Parameter: null}:
                    throw new InvalidOperationException("Bad object initializer expression: absence parameter info");
                case IArgumentOperation {Parameter: var parameterInfo}:
                    return ProcessWithLocalVariableCreation(parameterInfo.Name, conditionalExpr);
                default:
                    throw new InvalidOperationException("Bad object initializer expression: unknown operation info");
            }
        }

        private SyntaxNode ProcessWithLocalVariableCreation(String variableNamePrefix, ConditionalAccessExpressionSyntax conditionalExpr)
        {
            StatementSyntax statement = conditionalExpr.FindParentStatement().Must();
            String variableName = _variableManager.GenerateVariableName(statement, variableNamePrefix);
            String variableType = _model.ResolveExpressionType(conditionalExpr);
            SyntaxTrivia leadingSpaceTrivia = TriviaHelper.GetLeadingSpaceTrivia(statement);
            SyntaxTrivia eolTrivia = TriviaHelper.GetTrailingEndOfLineTrivia(statement);
            String defaultValue = TypeDefaultValueHelper.CreateDefaultValueForExpression(_model, conditionalExpr);
            String destLocalDeclaration = $"{leadingSpaceTrivia}{variableType} {variableName} = {defaultValue};{eolTrivia}";
            Dest.Add(destLocalDeclaration);
            IList<ExpressionSyntax> conditionalExprParts = SplitParts(conditionalExpr);
            AssignmentValuePartsProcessor partsProcessor = new AssignmentValuePartsProcessor(_model, _variableManager, statement, leadingSpaceTrivia, eolTrivia, variableName);
            Dest.AddRange(partsProcessor.ProcessParts(conditionalExprParts));
            return SyntaxFactory.IdentifierName(variableName);
        }

        // TODO (std_string) : think about more elegance solution
        private IList<ExpressionSyntax> SplitParts(ConditionalAccessExpressionSyntax conditionalAccessExpression)
        {
            IList<ExpressionSyntax> parts = new List<ExpressionSyntax>();
            while (true)
            {
                parts.Add(conditionalAccessExpression.Expression);
                switch (conditionalAccessExpression.WhenNotNull)
                {
                    case ConditionalAccessExpressionSyntax childAccessExpression:
                        conditionalAccessExpression = childAccessExpression;
                        break;
                    default:
                        parts.Add(conditionalAccessExpression.WhenNotNull);
                        return parts;
                }
            }
        }

        private readonly SemanticModel _model;
        private readonly VariableManager _variableManager;
    }

    internal abstract class ExpressionPartsProcessor
    {
        public ExpressionPartsProcessor(SemanticModel model, VariableManager variableManager, StatementSyntax parentStatement, SyntaxTrivia leadingSpaceTrivia, SyntaxTrivia eolTrivia)
        {
            ParentStatement = parentStatement;
            _model = model;
            _variableManager = variableManager;
            _leadingSpaceTrivia = leadingSpaceTrivia;
            _leadingSpaceDelta = TriviaHelper.CalcLeadingSpaceDelta(parentStatement);
            _eolTrivia = eolTrivia;
        }

        public String[] ProcessParts(IList<ExpressionSyntax> conditionalExprParts)
        {
            return ProcessParts(conditionalExprParts, 0, "", _leadingSpaceTrivia);
        }

        protected abstract String ProcessLastPart(ExpressionSyntax lastPart, String conditionalExprPartPrefix, SyntaxTrivia leadingSpaceTrivia, SyntaxTrivia eolTrivia);

        private String[] ProcessParts(IList<ExpressionSyntax> conditionalExprParts, Int32 partIndex, String prevConditionalExprVariable, SyntaxTrivia leadingSpaceTrivia)
        {
            ExpressionSyntax conditionalExprPart = conditionalExprParts[partIndex];
            String conditionalExprPartPrefix = partIndex == 0 ? "" : $"{prevConditionalExprVariable}";
            Int32 lastPartIndex = conditionalExprParts.Count - 1;
            if (partIndex == lastPartIndex)
            {
                String lastPartStatement = ProcessLastPart(conditionalExprPart, conditionalExprPartPrefix, leadingSpaceTrivia, _eolTrivia);
                return new String[] {lastPartStatement};
            }
            String conditionalExprVariable = _variableManager.GenerateVariableName(ParentStatement, "condExpression");
            String conditionalExprType = _model.ResolveExpressionType(conditionalExprPart);
            String conditionalVariableDeclaration = $"{leadingSpaceTrivia}{conditionalExprType} {conditionalExprVariable} = " +
                                                    $"{conditionalExprPartPrefix}{conditionalExprPart};{_eolTrivia}";
            String ifExpression = $"{conditionalExprVariable} != null";
            SyntaxTrivia nextLeadingSpaceTrivia = TriviaHelper.ShiftRightLeadingSpaceTrivia(leadingSpaceTrivia, _leadingSpaceDelta);
            String[] bodyParts = ProcessParts(conditionalExprParts, partIndex + 1, conditionalExprVariable, nextLeadingSpaceTrivia);
            String body = String.Join("", bodyParts);
            String ifStatement = $"{leadingSpaceTrivia}if ({ifExpression}){_eolTrivia}{leadingSpaceTrivia}{{{_eolTrivia}{body}{leadingSpaceTrivia}}}{_eolTrivia}";
            return new String[] {conditionalVariableDeclaration, ifStatement};
        }

        protected readonly StatementSyntax ParentStatement;
        private readonly SemanticModel _model;
        private readonly VariableManager _variableManager;
        private readonly SyntaxTrivia _leadingSpaceTrivia;
        private readonly Int32 _leadingSpaceDelta;
        private readonly SyntaxTrivia _eolTrivia;
    }

    internal class AssignmentValuePartsProcessor : ExpressionPartsProcessor
    {
        public AssignmentValuePartsProcessor(SemanticModel model,
                                             VariableManager variableManager,
                                             StatementSyntax parentStatement,
                                             SyntaxTrivia leadingSpaceTrivia,
                                             SyntaxTrivia eolTrivia,
                                             String leftPartAssignment)
            : base(model, variableManager, parentStatement, leadingSpaceTrivia, eolTrivia)
        {
            _leftPartAssignment = leftPartAssignment;
        }

        public AssignmentValuePartsProcessor(SemanticModel model,
                                             VariableManager variableManager,
                                             StatementSyntax parentStatement,
                                             SyntaxTrivia leadingSpaceTrivia,
                                             SyntaxTrivia eolTrivia,
                                             SyntaxToken targetVariable)
            : this(model, variableManager, parentStatement, leadingSpaceTrivia, eolTrivia, targetVariable.Text)
        {
        }

        protected override String ProcessLastPart(ExpressionSyntax lastPart, String conditionalExprPartPrefix, SyntaxTrivia leadingSpaceTrivia, SyntaxTrivia eolTrivia)
        {
            SyntaxTriviaList leadingTrivia = TriviaHelper.ConstructLeadingTrivia(ParentStatement, leadingSpaceTrivia, eolTrivia);
            SyntaxTriviaList trailingTrivia = TriviaHelper.ConstructTrailingTrivia(ParentStatement, eolTrivia);
            return $"{leadingTrivia}{_leftPartAssignment} = {conditionalExprPartPrefix}{lastPart};{trailingTrivia}";
        }

        private readonly String _leftPartAssignment;
    }

    internal class SimpleStatementPartsProcessor : ExpressionPartsProcessor
    {
        public SimpleStatementPartsProcessor(SemanticModel model, VariableManager variableManager, StatementSyntax parentStatement, SyntaxTrivia leadingSpaceTrivia, SyntaxTrivia eolTrivia)
            : base(model, variableManager, parentStatement, leadingSpaceTrivia, eolTrivia)
        {
        }

        protected override string ProcessLastPart(ExpressionSyntax lastPart, String conditionalExprPartPrefix, SyntaxTrivia leadingSpaceTrivia, SyntaxTrivia eolTrivia)
        {
            SyntaxTriviaList leadingTrivia = TriviaHelper.ConstructLeadingTrivia(ParentStatement, leadingSpaceTrivia, eolTrivia);
            SyntaxTriviaList trailingTrivia = TriviaHelper.ConstructTrailingTrivia(ParentStatement, eolTrivia);
            return $"{leadingTrivia}{conditionalExprPartPrefix}{lastPart};{trailingTrivia}";
        }
    }

    internal static class ExpressionResolverHelper
    {
        public static String ResolveExpressionType(this SemanticModel model, ExpressionSyntax expression)
        {
            TypeInfo typeInfo = model.GetTypeInfo(expression);
            return typeInfo.Type switch
            {
                null => throw new InvalidOperationException("Unknown semantic info"),
                // TODO (std_string) : think about specifying of format
                var typeSymbol => typeSymbol.ToDisplayString()
            };
        }

        /*public static String ResolveExpressionType(this SemanticModel model, ExpressionSyntax expression)
        {
            SymbolInfo expressionInfo = model.GetSymbolInfo(expression);
            return GetExpressionType(expressionInfo.Symbol);
        }

        private static String GetExpressionType(ISymbol? symbol)
        {
            return symbol switch
            {
                null => throw new InvalidOperationException("Unknown semantic info"),
                // TODO (std_string) : think about specifying of format
                ITypeSymbol typeSymbol => typeSymbol.ToDisplayString(),
                IMethodSymbol {ReturnsVoid: true} => throw new InvalidOperationException("Expression type is void"),
                IMethodSymbol methodSymbol => GetExpressionType(methodSymbol.ReturnType),
                _ => throw new InvalidOperationException("Unsupported semantic info")
            };
        }*/
    }

    internal static class TypeDefaultValueHelper
    {
        public static String CreateDefaultValueForType(SemanticModel model, TypeSyntax type)
        {
            return CreateDefaultValue(model.GetTypeInfo(type));
        }

        public static String CreateDefaultValueForExpression(SemanticModel model, ExpressionSyntax expression)
        {
            return CreateDefaultValue(model.GetTypeInfo(expression));
        }

        private static String CreateDefaultValue(TypeInfo typeInfo)
        {
            return typeInfo.Type switch
            {
                null => throw new InvalidOperationException("Unknown semantic info"),
                // TODO (std_string) : think about specifying of format
                var typeSymbol => $"default({typeSymbol})"
            };
        }
    }
}
