using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceCodeSimplifierApp.Utils;

namespace SourceCodeSimplifierApp.Variables
{
    internal class VariablesCollector
    {
        public ISet<String> CollectExistingVariables(StatementSyntax currentStatement)
        {
            HashSet<String> existingVariables = new HashSet<String>();
            MemberDeclarationSyntax containedMember = MoveToContainedMember(currentStatement);
            existingVariables.AddRange(GetParameterNames(containedMember));
            IList<String> variableDeclarators = containedMember
                .DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .Select(d => d.Identifier.ToString())
                .ToList();
            existingVariables.AddRange(variableDeclarators);
            IList<String> declarationExpressions = containedMember
                .DescendantNodes()
                .OfType<DeclarationExpressionSyntax>()
                .Select(d => d.Designation)
                .OfType<SingleVariableDesignationSyntax>()
                .Select(d => d.Identifier.ToString())
                .ToList();
            existingVariables.AddRange(declarationExpressions);
            return existingVariables;
        }

        private MemberDeclarationSyntax MoveToContainedMember(StatementSyntax currentStatement)
        {
            SyntaxNode? current = currentStatement;
            while (current != null)
            {
                current = current.Parent;
                if (current is MemberDeclarationSyntax memberDeclaration)
                    return memberDeclaration;
            }
            throw new InvalidOperationException("Bad statement (without contained member)");
        }

        private IList<String> GetParameterNames(MemberDeclarationSyntax containedMember)
        {
            return containedMember switch
            {
                MethodDeclarationSyntax methodDeclaration => methodDeclaration
                    .ParameterList
                    .Parameters
                    .Select(p => p.Identifier.ToString())
                    .ToList(),
                ConstructorDeclarationSyntax ctorDeclaration => ctorDeclaration
                    .ParameterList
                    .Parameters
                    .Select(p => p.Identifier.ToString())
                    .ToList(),
                IndexerDeclarationSyntax indexerDeclaration => indexerDeclaration
                    .ParameterList
                    .Parameters
                    .Select(p => p.Identifier.ToString())
                    .Append("value")
                    .ToList(),
                PropertyDeclarationSyntax => new[]{"value"},
                _ => new List<String>()
            };
        }
    }
}
