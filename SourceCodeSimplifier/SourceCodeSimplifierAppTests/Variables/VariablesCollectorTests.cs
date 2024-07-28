using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SourceCodeSimplifierApp.Variables;
using SourceCodeSimplifierAppTests.TestUtils;

namespace SourceCodeSimplifierAppTests.Variables
{
    [TestFixture]
    public class VariablesCollectorTests
    {
        [Test]
        public void CollectVariablesInMethod()
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefs +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public void SomeMethod(int param1, string param2)\r\n" +
                                  "        {\r\n" +
                                  "            int var1 = 666;\r\n" +
                                  "            var1 += 1;\r\n" +
                                  "            string var2 = \"\";\r\n" +
                                  "            var2 += \"IDDQD\";\r\n" +
                                  "            OtherMethod(out int outVar1, out string outVar2);\r\n" +
                                  "            SomeData var3 = new SomeData(out int dataVar1, out string dataVar2);\r\n" +
                                  "            AnotherMethod(var3);\r\n" +
                                  "            AnotherMethod(new SomeData(out int dataVar3, out string dataVar4));\r\n" +
                                  "            {\r\n" +
                                  "                int innerVar1 = 666;\r\n" +
                                  "                innerVar1 += 1;\r\n" +
                                  "            }\r\n" +
                                  "            {\r\n" +
                                  "                int innerVar1 = 666;\r\n" +
                                  "                innerVar1 += 1;\r\n" +
                                  "            }\r\n" +
                                  "        }\r\n" +
                                  "        public void OtherMethod(out int param1, out string param2)\r\n" +
                                  "        {\r\n" +
                                  "            param1 = 666;\r\n" +
                                  "            param2 = \"IDDQD\";\r\n" +
                                  "        }\r\n" +
                                  "        public void AnotherMethod(SomeData data)\r\n" +
                                  "        {\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}";
            String[] expected = new[] {"param1", "param2", "var1", "var2", "var3", "outVar1", "outVar2", "dataVar1", "dataVar2", "dataVar3", "dataVar4", "innerVar1"};
            CollectVariablesInMember<MethodDeclarationSyntax>(source, expected, method => method.Identifier.Text == "SomeMethod");
        }

        [Test]
        public void CollectVariablesInCtor()
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefs +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public SomeClass(int param1, string param2)\r\n" +
                                  "        {\r\n" +
                                  "            int var1 = 666;\r\n" +
                                  "            var1 += 1;\r\n" +
                                  "            string var2 = \"\";\r\n" +
                                  "            var2 += \"IDDQD\";\r\n" +
                                  "            OtherMethod(out int outVar1, out string outVar2);\r\n" +
                                  "            SomeData var3 = new SomeData(out int dataVar1, out string dataVar2);\r\n" +
                                  "            AnotherMethod(var3);\r\n" +
                                  "            AnotherMethod(new SomeData(out int dataVar3, out string dataVar4));\r\n" +
                                  "            {\r\n" +
                                  "                int innerVar1 = 666;\r\n" +
                                  "                innerVar1 += 1;\r\n" +
                                  "            }\r\n" +
                                  "            {\r\n" +
                                  "                int innerVar1 = 666;\r\n" +
                                  "                innerVar1 += 1;\r\n" +
                                  "            }\r\n" +
                                  "        }\r\n" +
                                  "        public void OtherMethod(out int param1, out string param2)\r\n" +
                                  "        {\r\n" +
                                  "            param1 = 666;\r\n" +
                                  "            param2 = \"IDDQD\";\r\n" +
                                  "        }\r\n" +
                                  "        public void AnotherMethod(SomeData data)\r\n" +
                                  "        {\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}";
            String[] expected = new[] {"param1", "param2", "var1", "var2", "var3", "outVar1", "outVar2", "dataVar1", "dataVar2", "dataVar3", "dataVar4", "innerVar1"};
            CollectVariablesInMember<ConstructorDeclarationSyntax>(source, expected, ctor => ctor.Identifier.Text == "SomeClass");
        }

        [Test]
        public void CollectVariablesInPropertyWithGetOnly()
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefs +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public int SomeValue\r\n" +
                                  "        {\r\n" +
                                  "            get\r\n" +
                                  "            {\r\n" +
                                  "                int var1 = 666;\r\n" +
                                  "                var1 += 1;\r\n" +
                                  "                string var2 = \"\";\r\n" +
                                  "                var2 += \"IDDQD\";\r\n" +
                                  "                OtherMethod(out int outVar1, out string outVar2);\r\n" +
                                  "                SomeData var3 = new SomeData(out int dataVar1, out string dataVar2);\r\n" +
                                  "                AnotherMethod(var3);\r\n" +
                                  "                AnotherMethod(new SomeData(out int dataVar3, out string dataVar4));\r\n" +
                                  "                {\r\n" +
                                  "                    int innerVar1 = 666;\r\n" +
                                  "                    innerVar1 += 1;\r\n" +
                                  "                }\r\n" +
                                  "                {\r\n" +
                                  "                    int innerVar1 = 666;\r\n" +
                                  "                    innerVar1 += 1;\r\n" +
                                  "                }\r\n" +
                                  "                return _someValue;\r\n" +
                                  "            }\r\n" +
                                  "        }\r\n" +
                                  "        public void OtherMethod(out int param1, out string param2)\r\n" +
                                  "        {\r\n" +
                                  "            param1 = 666;\r\n" +
                                  "            param2 = \"IDDQD\";\r\n" +
                                  "        }\r\n" +
                                  "        public void AnotherMethod(SomeData data)\r\n" +
                                  "        {\r\n" +
                                  "        }\r\n" +
                                  "        private int _someValue = 0;\r\n" +
                                  "    }\r\n" +
                                  "}";
            String[] expected = new[] {"value", "var1", "var2", "var3", "outVar1", "outVar2", "dataVar1", "dataVar2", "dataVar3", "dataVar4", "innerVar1"};
            CollectVariablesInMember<PropertyDeclarationSyntax>(source, expected, property => property.Identifier.Text == "SomeValue");
        }

        [Test]
        public void CollectVariablesInPropertyWithSetOnly()
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefs +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public int SomeValue\r\n" +
                                  "        {\r\n" +
                                  "            set\r\n" +
                                  "            {\r\n" +
                                  "                int var1 = 666;\r\n" +
                                  "                var1 += 1;\r\n" +
                                  "                string var2 = \"\";\r\n" +
                                  "                var2 += \"IDDQD\";\r\n" +
                                  "                OtherMethod(out int outVar1, out string outVar2);\r\n" +
                                  "                SomeData var3 = new SomeData(out int dataVar1, out string dataVar2);\r\n" +
                                  "                AnotherMethod(var3);\r\n" +
                                  "                AnotherMethod(new SomeData(out int dataVar3, out string dataVar4));\r\n" +
                                  "                {\r\n" +
                                  "                    int innerVar1 = 666;\r\n" +
                                  "                    innerVar1 += 1;\r\n" +
                                  "                }\r\n" +
                                  "                {\r\n" +
                                  "                    int innerVar1 = 666;\r\n" +
                                  "                    innerVar1 += 1;\r\n" +
                                  "                }\r\n" +
                                  "                _someValue = value;\r\n" +
                                  "            }\r\n" +
                                  "        }\r\n" +
                                  "        public void OtherMethod(out int param1, out string param2)\r\n" +
                                  "        {\r\n" +
                                  "            param1 = 666;\r\n" +
                                  "            param2 = \"IDDQD\";\r\n" +
                                  "        }\r\n" +
                                  "        public void AnotherMethod(SomeData data)\r\n" +
                                  "        {\r\n" +
                                  "        }\r\n" +
                                  "        private int _someValue = 0;\r\n" +
                                  "    }\r\n" +
                                  "}";
            String[] expected = new[] {"value", "var1", "var2", "var3", "outVar1", "outVar2", "dataVar1", "dataVar2", "dataVar3", "dataVar4", "innerVar1"};
            CollectVariablesInMember<PropertyDeclarationSyntax>(source, expected, property => property.Identifier.Text == "SomeValue");
        }

        [Test]
        public void CollectVariablesInProperty()
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefs +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public int SomeValue\r\n" +
                                  "        {\r\n" +
                                  "            get\r\n" +
                                  "            {\r\n" +
                                  "                int var1 = 666;\r\n" +
                                  "                var1 += 1;\r\n" +
                                  "                OtherMethod(out int outVar1, out string outVar2);\r\n" +
                                  "                SomeData var3 = new SomeData(out int dataVar1, out string dataVar2);\r\n" +
                                  "                AnotherMethod(var3);\r\n" +
                                  "                {\r\n" +
                                  "                    int innerVar1 = 666;\r\n" +
                                  "                    innerVar1 += 1;\r\n" +
                                  "                }\r\n" +
                                  "                return _someValue;\r\n" +
                                  "            }\r\n" +
                                  "            set\r\n" +
                                  "            {\r\n" +
                                  "                string var2 = \"\";\r\n" +
                                  "                var2 += \"IDDQD\";\r\n" +
                                  "                AnotherMethod(new SomeData(out int dataVar3, out string dataVar4));\r\n" +
                                  "                {\r\n" +
                                  "                    int innerVar1 = 666;\r\n" +
                                  "                    innerVar1 += 1;\r\n" +
                                  "                }\r\n" +
                                  "                _someValue = value;\r\n" +
                                  "            }\r\n" +
                                  "        }\r\n" +
                                  "        public void OtherMethod(out int param1, out string param2)\r\n" +
                                  "        {\r\n" +
                                  "            param1 = 666;\r\n" +
                                  "            param2 = \"IDDQD\";\r\n" +
                                  "        }\r\n" +
                                  "        public void AnotherMethod(SomeData data)\r\n" +
                                  "        {\r\n" +
                                  "        }\r\n" +
                                  "        private int _someValue = 0;\r\n" +
                                  "    }\r\n" +
                                  "}";
            String[] expected = new[] {"value", "var1", "var2", "var3", "outVar1", "outVar2", "dataVar1", "dataVar2", "dataVar3", "dataVar4", "innerVar1"};
            CollectVariablesInMember<PropertyDeclarationSyntax>(source, expected, property => property.Identifier.Text == "SomeValue");
        }

        [Test]
        public void CollectVariablesInIndexerWithGetOnly()
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefs +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public int this[int index]\r\n" +
                                  "        {\r\n" +
                                  "            get\r\n" +
                                  "            {\r\n" +
                                  "                int var1 = 666;\r\n" +
                                  "                var1 += 1;\r\n" +
                                  "                string var2 = \"\";\r\n" +
                                  "                var2 += \"IDDQD\";\r\n" +
                                  "                OtherMethod(out int outVar1, out string outVar2);\r\n" +
                                  "                SomeData var3 = new SomeData(out int dataVar1, out string dataVar2);\r\n" +
                                  "                AnotherMethod(var3);\r\n" +
                                  "                AnotherMethod(new SomeData(out int dataVar3, out string dataVar4));\r\n" +
                                  "                {\r\n" +
                                  "                    int innerVar1 = 666;\r\n" +
                                  "                    innerVar1 += 1;\r\n" +
                                  "                }\r\n" +
                                  "                {\r\n" +
                                  "                    int innerVar1 = 666;\r\n" +
                                  "                    innerVar1 += 1;\r\n" +
                                  "                }\r\n" +
                                  "                return _someValue;\r\n" +
                                  "            }\r\n" +
                                  "        }\r\n" +
                                  "        public void OtherMethod(out int param1, out string param2)\r\n" +
                                  "        {\r\n" +
                                  "            param1 = 666;\r\n" +
                                  "            param2 = \"IDDQD\";\r\n" +
                                  "        }\r\n" +
                                  "        public void AnotherMethod(SomeData data)\r\n" +
                                  "        {\r\n" +
                                  "        }\r\n" +
                                  "        private int _someValue = 0;\r\n" +
                                  "    }\r\n" +
                                  "}";
            String[] expected = new[] {"index", "value", "var1", "var2", "var3", "outVar1", "outVar2", "dataVar1", "dataVar2", "dataVar3", "dataVar4", "innerVar1"};
            CollectVariablesInMember<IndexerDeclarationSyntax>(source, expected, _ => true);
        }

        [Test]
        public void CollectVariablesInIndexerWithSetOnly()
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefs +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public int this[int index]\r\n" +
                                  "        {\r\n" +
                                  "            set\r\n" +
                                  "            {\r\n" +
                                  "                int var1 = 666;\r\n" +
                                  "                var1 += 1;\r\n" +
                                  "                string var2 = \"\";\r\n" +
                                  "                var2 += \"IDDQD\";\r\n" +
                                  "                OtherMethod(out int outVar1, out string outVar2);\r\n" +
                                  "                SomeData var3 = new SomeData(out int dataVar1, out string dataVar2);\r\n" +
                                  "                AnotherMethod(var3);\r\n" +
                                  "                AnotherMethod(new SomeData(out int dataVar3, out string dataVar4));\r\n" +
                                  "                {\r\n" +
                                  "                    int innerVar1 = 666;\r\n" +
                                  "                    innerVar1 += 1;\r\n" +
                                  "                }\r\n" +
                                  "                {\r\n" +
                                  "                    int innerVar1 = 666;\r\n" +
                                  "                    innerVar1 += 1;\r\n" +
                                  "                }\r\n" +
                                  "                _someValue = value;\r\n" +
                                  "            }\r\n" +
                                  "        }\r\n" +
                                  "        public void OtherMethod(out int param1, out string param2)\r\n" +
                                  "        {\r\n" +
                                  "            param1 = 666;\r\n" +
                                  "            param2 = \"IDDQD\";\r\n" +
                                  "        }\r\n" +
                                  "        public void AnotherMethod(SomeData data)\r\n" +
                                  "        {\r\n" +
                                  "        }\r\n" +
                                  "        private int _someValue = 0;\r\n" +
                                  "    }\r\n" +
                                  "}";
            String[] expected = new[] {"index", "value", "var1", "var2", "var3", "outVar1", "outVar2", "dataVar1", "dataVar2", "dataVar3", "dataVar4", "innerVar1"};
            CollectVariablesInMember<IndexerDeclarationSyntax>(source, expected, _ => true);
        }

        [Test]
        public void CollectVariablesInIndexer()
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefs +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public int this[int index]\r\n" +
                                  "        {\r\n" +
                                  "            get\r\n" +
                                  "            {\r\n" +
                                  "                int var1 = 666;\r\n" +
                                  "                var1 += 1;\r\n" +
                                  "                OtherMethod(out int outVar1, out string outVar2);\r\n" +
                                  "                SomeData var3 = new SomeData(out int dataVar1, out string dataVar2);\r\n" +
                                  "                AnotherMethod(var3);\r\n" +
                                  "                {\r\n" +
                                  "                    int innerVar1 = 666;\r\n" +
                                  "                    innerVar1 += 1;\r\n" +
                                  "                }\r\n" +
                                  "                return _someValue;\r\n" +
                                  "            }\r\n" +
                                  "            set\r\n" +
                                  "            {\r\n" +
                                  "                string var2 = \"\";\r\n" +
                                  "                var2 += \"IDDQD\";\r\n" +
                                  "                AnotherMethod(new SomeData(out int dataVar3, out string dataVar4));\r\n" +
                                  "                {\r\n" +
                                  "                    int innerVar1 = 666;\r\n" +
                                  "                    innerVar1 += 1;\r\n" +
                                  "                }\r\n" +
                                  "                _someValue = value;\r\n" +
                                  "            }\r\n" +
                                  "        }\r\n" +
                                  "        public void OtherMethod(out int param1, out string param2)\r\n" +
                                  "        {\r\n" +
                                  "            param1 = 666;\r\n" +
                                  "            param2 = \"IDDQD\";\r\n" +
                                  "        }\r\n" +
                                  "        public void AnotherMethod(SomeData data)\r\n" +
                                  "        {\r\n" +
                                  "        }\r\n" +
                                  "        private int _someValue = 0;\r\n" +
                                  "    }\r\n" +
                                  "}";
            String[] expected = new[] {"index", "value", "var1", "var2", "var3", "outVar1", "outVar2", "dataVar1", "dataVar2", "dataVar3", "dataVar4", "innerVar1"};
            CollectVariablesInMember<IndexerDeclarationSyntax>(source, expected, _ => true);
        }

        private void CollectVariablesInMember<TMemberDeclaration>(String source, String[] expected, Func<TMemberDeclaration, Boolean> memberNameSelector)
            where TMemberDeclaration : MemberDeclarationSyntax
        {
            ISet<String> expectedVariables = new HashSet<String>(expected);
            Document sourceDocument = PreparationHelper.Prepare(source, "VariablesCollector");
            SyntaxNode? root = sourceDocument.GetSyntaxRootAsync().Result;
            Assert.That(root, Is.Not.Null);
            TMemberDeclaration? selectedMember = root!.DescendantNodes().OfType<TMemberDeclaration>().FirstOrDefault(memberNameSelector);
            Assert.That(selectedMember, Is.Not.Null);
            StatementSyntax[] methodStatements = selectedMember!.DescendantNodes().OfType<StatementSyntax>().ToArray();
            VariablesCollector variablesCollector = new VariablesCollector();
            foreach (StatementSyntax methodStatement in methodStatements)
            {
                ISet<String> actualVariables = variablesCollector.CollectExistingVariables(methodStatement);
                Assert.That(actualVariables, Is.EquivalentTo(expectedVariables));
            }
        }

        private const String CommonDefs = "    public class SomeData\r\n" +
                                          "    {\r\n" +
                                          "        public SomeData(out int param1, out string param2)\r\n" +
                                          "        {\r\n" +
                                          "            param1 = 666;\r\n" +
                                          "            param2 = \"IDDQD\";\r\n" +
                                          "        }\r\n" +
                                          "    }\r\n";
    }
}
