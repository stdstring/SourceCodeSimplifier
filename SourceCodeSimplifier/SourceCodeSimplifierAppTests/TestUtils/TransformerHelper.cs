using Microsoft.CodeAnalysis;
using NUnit.Framework;
using SourceCodeSimplifierApp.Config;
using SourceCodeSimplifierApp.Output;
using SourceCodeSimplifierApp.Transformers;

namespace SourceCodeSimplifierAppTests.TestUtils
{
    internal class TransformerHelper
    {
        public TransformerHelper(String source, String namePrefix, OutputLevel outputLevel)
        {
            _source = source;
            _namePrefix = namePrefix;
            _outputLevel = outputLevel;
        }

        public void Process(Func<IOutput, ITransformer> transformerFactory, String expectedOutput, String expectedResult)
        {
            Document sourceDocument = PreparationHelper.Prepare(_source, _namePrefix);
            using (TextWriter outputWriter = new StringWriter())
            using (TextWriter errorWriter = new StringWriter())
            {
                OutputImpl output = new OutputImpl(outputWriter, errorWriter, _outputLevel);
                ITransformer transformer = transformerFactory(output);
                Document destDocument = transformer.Transform(sourceDocument);
                String actualOutput = outputWriter.ToString() ?? "";
                String actualError = errorWriter.ToString() ?? "";
                String actualResult = destDocument.GetTextAsync().Result.ToString();
                Assert.Multiple(() =>
                {
                    Assert.That(actualResult, Is.EqualTo(expectedResult));
                    Assert.That(actualOutput, Is.EqualTo(expectedOutput));
                    Assert.That(actualError, Is.Empty);
                });
            }
        }

        private readonly String _source;
        private readonly String _namePrefix;
        private readonly OutputLevel _outputLevel;
    }
}
