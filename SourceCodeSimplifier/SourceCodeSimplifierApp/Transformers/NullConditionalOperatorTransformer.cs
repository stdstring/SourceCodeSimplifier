using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
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
            String filename = source.FilePath ?? source.Name;
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
                IList<StatementSyntax> destStatements = new List<StatementSyntax>();
                NullConditionalOperatorRewriter rewriter = new NullConditionalOperatorRewriter(model, variableManager, destStatements, _output, filename);
                rewriter.Visit(parentStatement);
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
        public NullConditionalOperatorRewriter(SemanticModel model,
                                               VariableManager variableManager,
                                               IList<StatementSyntax> destStatements,
                                               IOutput output,
                                               String filename)
        {
            _model = model;
            _variableManager = variableManager;
            _destStatements = destStatements;
            _output = output;
            _filename = filename;
        }

        private readonly SemanticModel _model;
        private readonly IList<StatementSyntax> _destStatements;
        private readonly VariableManager _variableManager;
        private readonly IOutput _output;
        private readonly String _filename;
    }
}
