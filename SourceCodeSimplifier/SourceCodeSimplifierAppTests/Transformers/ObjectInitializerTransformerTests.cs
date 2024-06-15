using NUnit.Framework;
using SourceCodeSimplifierApp.Config;
using SourceCodeSimplifierApp.Output;
using SourceCodeSimplifierApp.Transformers;
using SourceCodeSimplifierAppTests.TestUtils;

namespace SourceCodeSimplifierAppTests.Transformers
{
    [TestFixture]
    public class ObjectInitializerTransformerTests
    {
        [TestCase(OutputLevel.Error)]
        [TestCase(OutputLevel.Warning)]
        [TestCase(OutputLevel.Info)]
        public void ProcessWithObjectInitializerExpressions(OutputLevel outputLevel)
        {
            const String commonDefinitions = "    public class SomeOuterData\r\n" +
                                             "    {\r\n" +
                                             "        public SomeOuterData(int outerField1, string outerField2, bool outerField3)\r\n" +
                                             "        {\r\n" +
                                             "            OuterField1 = outerField1;\r\n" +
                                             "            OuterField2 = outerField2;\r\n" +
                                             "            OuterField3 = outerField3;\r\n" +
                                             "            InnerData = new SomeInnerData(777, \"IDKFA\");\r\n" +
                                             "        }\r\n" +
                                             "        public int OuterField1;\r\n" +
                                             "        public string OuterField2;\r\n" +
                                             "        public bool OuterField3;\r\n" +
                                             "        public SomeInnerData InnerData;\r\n" +
                                             "    }\r\n" +
                                             "    public class SomeInnerData\r\n" +
                                             "    {\r\n" +
                                             "        public SomeInnerData(int innerField1, string innerField2)\r\n" +
                                             "        {\r\n" +
                                             "            InnerField1 = innerField1;\r\n" +
                                             "            InnerField2 = innerField2;\r\n" +
                                             "        }\r\n" +
                                             "        public int InnerField1;\r\n" +
                                             "        public string InnerField2;\r\n" +
                                             "    }\r\n";
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  commonDefinitions +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public void SomeMethod(int i)\r\n" +
                                  "        {\r\n" +
                                  "            SomeOuterData data = new SomeOuterData(1, \"-\", false)\r\n" +
                                  "            {\r\n" +
                                  "                OuterField1 = 666,\r\n" +
                                  "                OuterField2 = \"IDDQD\",\r\n" +
                                  "                OuterField3 = true,\r\n" +
                                  "                InnerData = new SomeInnerData(13, \"---\")\r\n" +
                                  "                {\r\n" +
                                  "                    InnerField1 = 888,\r\n" +
                                  "                    InnerField2 = \"IDCLIP\"\r\n" +
                                  "                }\r\n" +
                                  "            };\r\n" +
                                  "            if (i > 0)\r\n" +
                                  "            {\r\n" +
                                  "                data.InnerData = new SomeInnerData(73, \"+-+\")\r\n" +
                                  "                {\r\n" +
                                  "                    InnerField1 = 22227,\r\n" +
                                  "                    InnerField2 = \"DNMD\"\r\n" +
                                  "                };\r\n" +
                                  "            }\r\n" +
                                  "            else\r\n" +
                                  "                data.InnerData = new SomeInnerData(23, \"++\")\r\n" +
                                  "                {\r\n" +
                                  "                    InnerField1 = 11117,\r\n" +
                                  "                    InnerField2 = \"DNMD\"\r\n" +
                                  "                };\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}\r\n";
            const String expectedResult = "namespace SomeNamespace\r\n" +
                                          "{\r\n" +
                                          commonDefinitions +
                                          "    public class SomeClass\r\n" +
                                          "    {\r\n" +
                                          "        public void SomeMethod(int i)\r\n" +
                                          "        {\r\n" +
                                          "            SomeOuterData data = new SomeOuterData(1, \"-\", false);\r\n" +
                                          "            data.OuterField1 = 666;\r\n" +
                                          "            data.OuterField2 = \"IDDQD\";\r\n" +
                                          "            data.OuterField3 = true;\r\n" +
                                          "            data.InnerData = new SomeInnerData(13, \"---\");\r\n" +
                                          "            data.InnerData.InnerField1 = 888;\r\n" +
                                          "            data.InnerData.InnerField2 = \"IDCLIP\";\r\n" +
                                          "            if (i > 0)\r\n" +
                                          "            {\r\n" +
                                          "                data.InnerData = new SomeInnerData(73, \"+-+\");\r\n" +
                                          "                data.InnerData.InnerField1 = 22227;\r\n" +
                                          "                data.InnerData.InnerField2 = \"DNMD\";\r\n" +
                                          "            }\r\n" +
                                          "            else\r\n" +
                                          "            {\r\n" +
                                          "                data.InnerData = new SomeInnerData(23, \"++\");\r\n" +
                                          "                data.InnerData.InnerField1 = 11117;\r\n" +
                                          "                data.InnerData.InnerField2 = \"DNMD\";\r\n" +
                                          "            }\r\n" +
                                          "        }\r\n" +
                                          "    }\r\n" +
                                          "}\r\n";
            String expectedOutput = outputLevel == OutputLevel.Info ? ExpectedOutputForInfoLevel : "";
            TransformerHelper transformerHelper = new TransformerHelper(source, "ObjectInitializerExpression", outputLevel);
            transformerHelper.Process(_transformerOnFactory, expectedOutput, expectedResult);
            transformerHelper.Process(_transformerOffFactory, "", source);
        }

        [TestCase(OutputLevel.Error)]
        [TestCase(OutputLevel.Warning)]
        [TestCase(OutputLevel.Info)]
        public void ProcessWithoutObjectInitializerExpressions(OutputLevel outputLevel)
        {
            const String commonDefinitions = "    public class SomeOuterData\r\n" +
                                             "    {\r\n" +
                                             "        public SomeOuterData(int outerField1, string outerField2, bool outerField3)\r\n" +
                                             "        {\r\n" +
                                             "            OuterField1 = outerField1;\r\n" +
                                             "            OuterField2 = outerField2;\r\n" +
                                             "            OuterField3 = outerField3;\r\n" +
                                             "            InnerData = new SomeInnerData(777, \"IDKFA\");\r\n" +
                                             "        }\r\n" +
                                             "        public int OuterField1;\r\n" +
                                             "        public string OuterField2;\r\n" +
                                             "        public bool OuterField3;\r\n" +
                                             "        public SomeInnerData InnerData;\r\n" +
                                             "    }\r\n" +
                                             "    public class SomeInnerData\r\n" +
                                             "    {\r\n" +
                                             "        public SomeInnerData(int innerField1, string innerField2)\r\n" +
                                             "        {\r\n" +
                                             "            InnerField1 = innerField1;\r\n" +
                                             "            InnerField2 = innerField2;\r\n" +
                                             "        }\r\n" +
                                             "        public int InnerField1;\r\n" +
                                             "        public string InnerField2;\r\n" +
                                             "    }\r\n";
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  commonDefinitions +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public void SomeMethod(int i)\r\n" +
                                  "        {\r\n" +
                                  "            SomeOuterData data = new SomeOuterData(1, \"-\", false);\r\n" +
                                  "            data.OuterField1 = 666;\r\n" +
                                  "            data.OuterField2 = \"IDDQD\";\r\n" +
                                  "            data.OuterField3 = true;\r\n" +
                                  "            data.InnerData = new SomeInnerData(13, \"---\");\r\n" +
                                  "            data.InnerData.InnerField1 = 888;\r\n" +
                                  "            data.InnerData.InnerField2 = \"IDCLIP\";\r\n" +
                                  "            if (i > 0)\r\n" +
                                  "            {\r\n" +
                                  "                data.InnerData = new SomeInnerData(73, \"+-+\");\r\n" +
                                  "                data.InnerData.InnerField1 = 22227;\r\n" +
                                  "                data.InnerData.InnerField2 = \"DNMD\";\r\n" +
                                  "            }\r\n" +
                                  "            else\r\n" +
                                  "            {\r\n" +
                                  "                data.InnerData = new SomeInnerData(23, \"++\");\r\n" +
                                  "                data.InnerData.InnerField1 = 11117;\r\n" +
                                  "                data.InnerData.InnerField2 = \"DNMD\";\r\n" +
                                  "            }\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}\r\n";
            String expectedOutput = outputLevel == OutputLevel.Info ? ExpectedOutputForInfoLevel : "";
            TransformerHelper transformerHelper = new TransformerHelper(source, "ObjectInitializerExpression", outputLevel);
            transformerHelper.Process(_transformerOnFactory, expectedOutput, source);
            transformerHelper.Process(_transformerOffFactory, "", source);
        }

        [TestCase(OutputLevel.Error)]
        [TestCase(OutputLevel.Warning)]
        [TestCase(OutputLevel.Info)]
        public void ProcessWithObjectInitializerExpressionsInMethodCall(OutputLevel outputLevel)
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  "    public class SomeData\r\n" +
                                  "    {\r\n" +
                                  "        public SomeData(int field1, string field2, bool field3)\r\n" +
                                  "        {\r\n" +
                                  "            Field1 = field1;\r\n" +
                                  "            Field2 = field2;\r\n" +
                                  "            Field3 = field3;\r\n" +
                                  "        }\r\n" +
                                  "        public int Field1;\r\n" +
                                  "        public string Field2;\r\n" +
                                  "        public bool Field3;\r\n" +
                                  "    }\r\n" +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public void SomeMethod()\r\n" +
                                  "        {\r\n" +
                                  "            OtherMethod(new SomeData(1, \"==\", false){Field1 = 666, Field2 = \"IDDQD\"});\r\n" +
                                  "        }\r\n" +
                                  "        public void OtherMethod(SomeData data)\r\n" +
                                  "        {\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}";
            TransformerHelper transformerHelper = new TransformerHelper(source, "ObjectInitializerExpression", outputLevel);
            transformerHelper.ProcessWithException<InvalidOperationException>(_transformerOnFactory);
            transformerHelper.Process(_transformerOffFactory, "", source);
        }

        [TestCase(OutputLevel.Error)]
        [TestCase(OutputLevel.Warning)]
        [TestCase(OutputLevel.Info)]
        public void ProcessWithObjectInitializerExpressionsInReturnStatement(OutputLevel outputLevel)
        {
            const String source = "namespace SomeNamespace\r\n" +
                                  "{\r\n" +
                                  "    public class SomeData\r\n" +
                                  "    {\r\n" +
                                  "        public SomeData(int field1, string field2, bool field3)\r\n" +
                                  "        {\r\n" +
                                  "            Field1 = field1;\r\n" +
                                  "            Field2 = field2;\r\n" +
                                  "            Field3 = field3;\r\n" +
                                  "        }\r\n" +
                                  "        public int Field1;\r\n" +
                                  "        public string Field2;\r\n" +
                                  "        public bool Field3;\r\n" +
                                  "    }\r\n" +
                                  "    public class SomeClass\r\n" +
                                  "    {\r\n" +
                                  "        public SomeData SomeMethod()\r\n" +
                                  "        {\r\n" +
                                  "            return new SomeData(1, \"==\", false){Field1 = 666, Field2 = \"IDDQD\"};\r\n" +
                                  "        }\r\n" +
                                  "    }\r\n" +
                                  "}";
            TransformerHelper transformerHelper = new TransformerHelper(source, "ObjectInitializerExpression", outputLevel);
            transformerHelper.ProcessWithException<InvalidOperationException>(_transformerOnFactory);
            transformerHelper.Process(_transformerOffFactory, "", source);
        }

        private readonly Func<IOutput, ITransformer> _transformerOnFactory = output => new ObjectInitializerTransformer(output, TransformerState.On);
        private readonly Func<IOutput, ITransformer> _transformerOffFactory = output => new ObjectInitializerTransformer(output, TransformerState.Off);

        private const String ExpectedOutputForInfoLevel = $"Execution of {ObjectInitializerTransformer.Name} started\r\n" +
                                                          $"Execution of {ObjectInitializerTransformer.Name} finished\r\n";
    }
}
