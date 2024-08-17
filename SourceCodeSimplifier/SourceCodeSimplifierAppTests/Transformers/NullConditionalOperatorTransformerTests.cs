using NUnit.Framework;
using SourceCodeSimplifierApp.Config;
using SourceCodeSimplifierApp.Output;
using SourceCodeSimplifierApp.Transformers;
using SourceCodeSimplifierAppTests.TestUtils;

namespace SourceCodeSimplifierAppTests.Transformers
{
    [TestFixture]
    public class NullConditionalOperatorTransformerTests
    {
        [TestCase(OutputLevel.Error)]
        [TestCase(OutputLevel.Warning)]
        [TestCase(OutputLevel.Info)]
        public void ProcessNullConditionalOperatorInLocalVariableDeclaration(OutputLevel outputLevel)
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefinitions +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public void SomeMethod()\r\n" +
                                  "        {\r\n" +
                                  "            AnotherData anotherData = new AnotherData();\r\n" +
                                  "            string value = anotherData.CreateOtherData()?.CreateSomeData()?.CreateStr();\r\n" +
                                  "            string value2 = anotherData.CreateSeveralOtherData()[2]?.CreateSomeData()?.CreateStr();\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}";
            const String expectedResult = "";
            String expectedOutput = outputLevel == OutputLevel.Info ? ExpectedOutputForInfoLevel : "";
            TransformerHelper transformerHelper = new TransformerHelper(source, "ObjectInitializerExpression", outputLevel);
            transformerHelper.Process(_transformerOnFactory, expectedOutput, expectedResult);
            transformerHelper.Process(_transformerOffFactory, "", source);
        }

        [TestCase(OutputLevel.Error)]
        [TestCase(OutputLevel.Warning)]
        [TestCase(OutputLevel.Info)]
        public void ProcessNullConditionalOperatorInMethodCall(OutputLevel outputLevel)
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefinitions +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public void SomeMethod()\r\n" +
                                  "        {\r\n" +
                                  "            AnotherData anotherData = new AnotherData();\r\n" +
                                  "            anotherData.CreateOtherData()?.CreateSomeData()?.DoIt();\r\n" +
                                  "            anotherData.CreateSeveralOtherData()[3]?.CreateSomeData()?.DoIt();\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}";
            const String expectedResult = "";
            String expectedOutput = outputLevel == OutputLevel.Info ? ExpectedOutputForInfoLevel : "";
            TransformerHelper transformerHelper = new TransformerHelper(source, "ObjectInitializerExpression", outputLevel);
            transformerHelper.Process(_transformerOnFactory, expectedOutput, expectedResult);
            transformerHelper.Process(_transformerOffFactory, "", source);
        }

        [TestCase(OutputLevel.Error)]
        [TestCase(OutputLevel.Warning)]
        [TestCase(OutputLevel.Info)]
        public void ProcessNullConditionalOperatorWithNullCoalescingExpr(OutputLevel outputLevel)
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefinitions +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public void SomeMethod()\r\n" +
                                  "        {\r\n" +
                                  "            AnotherData anotherData = new AnotherData();\r\n" +
                                  "            string value = anotherData.CreateOtherData()?.CreateSomeData()?.CreateStr() ?? \"\";\r\n" +
                                  "            string value2 = anotherData.CreateOtherData()?.CreateSomeData()?.CreateStr() ?? CreateDefaultStr() ?? \"\";\r\n" +
                                  "        }\r\n" +
                                  "        public string CreateDefaultStr()\r\n" +
                                  "        {\r\n" +
                                  "            return \"\";\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}";
            const String expectedResult = "";
            String expectedOutput = outputLevel == OutputLevel.Info ? ExpectedOutputForInfoLevel : "";
            TransformerHelper transformerHelper = new TransformerHelper(source, "ObjectInitializerExpression", outputLevel);
            transformerHelper.Process(_transformerOnFactory, expectedOutput, expectedResult);
            transformerHelper.Process(_transformerOffFactory, "", source);
        }

        [TestCase(OutputLevel.Error)]
        [TestCase(OutputLevel.Warning)]
        [TestCase(OutputLevel.Info)]
        public void ProcessNullConditionalOperatorWithNullCoalescingInnerExpr(OutputLevel outputLevel)
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefinitions +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public void SomeMethod()\r\n" +
                                  "        {\r\n" +
                                  "            AnotherData anotherData = new AnotherData();\r\n" +
                                  "            string value = (anotherData.CreateOtherData() ?? CreateDefaultOtherData() ?? CreateDefaultOtherData2())?.CreateSomeData()?.CreateStr();\r\n" +
                                  "            (anotherData.CreateOtherData() ?? CreateDefaultOtherData() ?? CreateDefaultOtherData2())?.CreateSomeData()?.DoIt();\r\n" +
                                  "        }\r\n" +
                                  "        public string CreateDefaultStr()\r\n" +
                                  "        {\r\n" +
                                  "            return \"\";\r\n" +
                                  "        }\r\n" +
                                  "        public OtherData CreateDefaultOtherData()\r\n" +
                                  "        {\r\n" +
                                  "            return new OtherData();\r\n" +
                                  "        }\r\n" +
                                  "        public OtherData CreateDefaultOtherData2()\r\n" +
                                  "        {\r\n" +
                                  "            return new OtherData();\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}";
            const String expectedResult = "";
            String expectedOutput = outputLevel == OutputLevel.Info ? ExpectedOutputForInfoLevel : "";
            TransformerHelper transformerHelper = new TransformerHelper(source, "ObjectInitializerExpression", outputLevel);
            transformerHelper.Process(_transformerOnFactory, expectedOutput, expectedResult);
            transformerHelper.Process(_transformerOffFactory, "", source);
        }

        [TestCase(OutputLevel.Error)]
        [TestCase(OutputLevel.Warning)]
        [TestCase(OutputLevel.Info)]
        public void ProcessNullConditionalOperatorInMethodArg(OutputLevel outputLevel)
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefinitions +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public void SomeMethod()\r\n" +
                                  "        {\r\n" +
                                  "            AnotherData anotherData = new AnotherData();\r\n" +
                                  "            OtherMethod(anotherData.CreateOtherData()?.CreateSomeData()?.CreateStr());\r\n" +
                                  "            OtherMethod(anotherData.CreateSeveralOtherData()[2]?.CreateSomeData()?.CreateStr());\r\n" +
                                  "        }\r\n" +
                                  "        public void OtherMethod(string str)\r\n" +
                                  "        {\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}";
            const String expectedResult = "";
            String expectedOutput = outputLevel == OutputLevel.Info ? ExpectedOutputForInfoLevel : "";
            TransformerHelper transformerHelper = new TransformerHelper(source, "ObjectInitializerExpression", outputLevel);
            transformerHelper.Process(_transformerOnFactory, expectedOutput, expectedResult);
            transformerHelper.Process(_transformerOffFactory, "", source);
        }

        [TestCase(OutputLevel.Error)]
        [TestCase(OutputLevel.Warning)]
        [TestCase(OutputLevel.Info)]
        public void ProcessNullConditionalOperatorWithNullCoalescingExprInMethodArg(OutputLevel outputLevel)
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefinitions +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public void SomeMethod()\r\n" +
                                  "        {\r\n" +
                                  "            AnotherData anotherData = new AnotherData();\r\n" +
                                  "            OtherMethod(anotherData.CreateOtherData()?.CreateSomeData()?.CreateStr() ?? \"\");\r\n" +
                                  "            OtherMethod(anotherData.CreateOtherData()?.CreateSomeData()?.CreateStr() ?? CreateDefaultStr() ?? \"\");\r\n" +
                                  "        }\r\n" +
                                  "        public string CreateDefaultStr()\r\n" +
                                  "        {\r\n" +
                                  "            return \"\";\r\n" +
                                  "        }\r\n" +
                                  "        public void OtherMethod(string str)\r\n" +
                                  "        {\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}";
            const String expectedResult = "";
            String expectedOutput = outputLevel == OutputLevel.Info ? ExpectedOutputForInfoLevel : "";
            TransformerHelper transformerHelper = new TransformerHelper(source, "ObjectInitializerExpression", outputLevel);
            transformerHelper.Process(_transformerOnFactory, expectedOutput, expectedResult);
            transformerHelper.Process(_transformerOffFactory, "", source);
        }

        [TestCase(OutputLevel.Error)]
        [TestCase(OutputLevel.Warning)]
        [TestCase(OutputLevel.Info)]
        public void ProcessNullConditionalOperatorWithNullCoalescingInnerExprInMethodArg(OutputLevel outputLevel)
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefinitions +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public void SomeMethod()\r\n" +
                                  "        {\r\n" +
                                  "            AnotherData anotherData = new AnotherData();\r\n" +
                                  "            OtherMethod((anotherData.CreateOtherData() ?? CreateDefaultOtherData() ?? CreateDefaultOtherData2())?.CreateSomeData()?.CreateStr());\r\n" +
                                  "            OtherMethod((anotherData.CreateOtherData() ?? CreateDefaultOtherData() ?? CreateDefaultOtherData2())?.CreateSomeData()?.CreateStr() ?? \"\");\r\n" +
                                  "        }\r\n" +
                                  "        public string CreateDefaultStr()\r\n" +
                                  "        {\r\n" +
                                  "            return \"\";\r\n" +
                                  "        }\r\n" +
                                  "        public OtherData CreateDefaultOtherData()\r\n" +
                                  "        {\r\n" +
                                  "            return new OtherData();\r\n" +
                                  "        }\r\n" +
                                  "        public OtherData CreateDefaultOtherData2()\r\n" +
                                  "        {\r\n" +
                                  "            return new OtherData();\r\n" +
                                  "        }\r\n" +
                                  "        public void OtherMethod(string str)\r\n" +
                                  "        {\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}";
            const String expectedResult = "";
            String expectedOutput = outputLevel == OutputLevel.Info ? ExpectedOutputForInfoLevel : "";
            TransformerHelper transformerHelper = new TransformerHelper(source, "ObjectInitializerExpression", outputLevel);
            transformerHelper.Process(_transformerOnFactory, expectedOutput, expectedResult);
            transformerHelper.Process(_transformerOffFactory, "", source);
        }

        [TestCase(OutputLevel.Error)]
        [TestCase(OutputLevel.Warning)]
        [TestCase(OutputLevel.Info)]
        public void ProcessNullConditionalOperatorInReturn(OutputLevel outputLevel)
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefinitions +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public string AnotherMethod(AnotherData anotherData)\r\n" +
                                  "        {\r\n" +
                                  "            return anotherData.CreateOtherData()?.CreateSomeData()?.CreateStr();\r\n" +
                                  "        }\r\n" +
                                  "        public string AnotherMethod2(AnotherData anotherData)\r\n" +
                                  "        {\r\n" +
                                  "            return anotherData.CreateSeveralOtherData()[2]?.CreateSomeData()?.CreateStr();\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}";
            const String expectedResult = "";
            String expectedOutput = outputLevel == OutputLevel.Info ? ExpectedOutputForInfoLevel : "";
            TransformerHelper transformerHelper = new TransformerHelper(source, "ObjectInitializerExpression", outputLevel);
            transformerHelper.Process(_transformerOnFactory, expectedOutput, expectedResult);
            transformerHelper.Process(_transformerOffFactory, "", source);
        }

        [TestCase(OutputLevel.Error)]
        [TestCase(OutputLevel.Warning)]
        [TestCase(OutputLevel.Info)]
        public void ProcessNullConditionalOperatorWithNullCoalescingExprInReturn(OutputLevel outputLevel)
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefinitions +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public string AnotherMethod(AnotherData anotherData)\r\n" +
                                  "        {\r\n" +
                                  "            return anotherData.CreateOtherData()?.CreateSomeData()?.CreateStr() ?? \"\";\r\n" +
                                  "        }\r\n" +
                                  "        public string AnotherMethod2(AnotherData anotherData)\r\n" +
                                  "        {\r\n" +
                                  "            return anotherData.CreateOtherData()?.CreateSomeData()?.CreateStr() ?? CreateDefaultStr() ?? \"\";\r\n" +
                                  "        }\r\n" +
                                  "        public string CreateDefaultStr()\r\n" +
                                  "        {\r\n" +
                                  "            return \"\";\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}";
            const String expectedResult = "";
            String expectedOutput = outputLevel == OutputLevel.Info ? ExpectedOutputForInfoLevel : "";
            TransformerHelper transformerHelper = new TransformerHelper(source, "ObjectInitializerExpression", outputLevel);
            transformerHelper.Process(_transformerOnFactory, expectedOutput, expectedResult);
            transformerHelper.Process(_transformerOffFactory, "", source);
        }

        [TestCase(OutputLevel.Error)]
        [TestCase(OutputLevel.Warning)]
        [TestCase(OutputLevel.Info)]
        public void ProcessNullConditionalOperatorWithNullCoalescingInnerExprInReturn(OutputLevel outputLevel)
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  CommonDefinitions +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public string AnotherMethod(AnotherData anotherData)\r\n" +
                                  "        {\r\n" +
                                  "            return (anotherData.CreateOtherData() ?? CreateDefaultOtherData() ?? CreateDefaultOtherData2())?.CreateSomeData()?.CreateStr();\r\n" +
                                  "        }\r\n" +
                                  "        public string AnotherMethod2(AnotherData anotherData)\r\n" +
                                  "        {\r\n" +
                                  "            return (anotherData.CreateOtherData() ?? CreateDefaultOtherData() ?? CreateDefaultOtherData2())?.CreateSomeData()?.CreateStr() ?? \"\";\r\n" +
                                  "        }\r\n" +
                                  "        public string CreateDefaultStr()\r\n" +
                                  "        {\r\n" +
                                  "            return \"\";\r\n" +
                                  "        }\r\n" +
                                  "        public OtherData CreateDefaultOtherData()\r\n" +
                                  "        {\r\n" +
                                  "            return new OtherData();\r\n" +
                                  "        }\r\n" +
                                  "        public OtherData CreateDefaultOtherData2()\r\n" +
                                  "        {\r\n" +
                                  "            return new OtherData();\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}";
            const String expectedResult = "";
            String expectedOutput = outputLevel == OutputLevel.Info ? ExpectedOutputForInfoLevel : "";
            TransformerHelper transformerHelper = new TransformerHelper(source, "ObjectInitializerExpression", outputLevel);
            transformerHelper.Process(_transformerOnFactory, expectedOutput, expectedResult);
            transformerHelper.Process(_transformerOffFactory, "", source);
        }

        private readonly Func<IOutput, ITransformer> _transformerOnFactory = output => new ObjectInitializerExprTransformer(output, TransformerState.On);
        private readonly Func<IOutput, ITransformer> _transformerOffFactory = output => new ObjectInitializerExprTransformer(output, TransformerState.Off);

        private const String CommonDefinitions = "    public class SomeData\r\n" +
                                                 "    {\r\n" +
                                                 "        public void DoIt()\r\n" +
                                                 "        {\r\n" +
                                                 "        }\r\n" +
                                                 "        public string CreateStr()\r\n" +
                                                 "        {\r\n" +
                                                 "            return \"\";\r\n" +
                                                 "        }\r\n" +
                                                 "    }\r\n" +
                                                 "    public class OtherData\r\n" +
                                                 "    {\r\n" +
                                                 "        public SomeData CreateSomeData()\r\n" +
                                                 "        {\r\n" +
                                                 "            return new SomeData();\r\n" +
                                                 "        }\r\n" +
                                                 "    }\r\n" +
                                                 "    public class AnotherData\r\n" +
                                                 "    {\r\n" +
                                                 "        public OtherData CreateOtherData()\r\n" +
                                                 "        {\r\n" +
                                                 "            return new OtherData();\r\n" +
                                                 "        }\r\n" +
                                                 "        public OtherData[] CreateSeveralOtherData()\r\n" +
                                                 "        {\r\n" +
                                                 "            return new OtherData[10];\r\n" +
                                                 "        }\r\n" +
                                                 "    }\r\n";

        private const String ExpectedOutputForInfoLevel = $"Execution of {NullConditionalOperatorTransformer.Name} started\r\n" +
                                                          $"Execution of {NullConditionalOperatorTransformer.Name} finished\r\n";
    }
}
