using SourceCodeSimplifierApp.Config;
using SourceCodeSimplifierApp.Output;

namespace SourceCodeSimplifierApp.Transformers
{
    internal static class TransformersFactory
    {
        public static IList<ITransformer> Create(IOutput output, TransformerEntry[] config)
        {
            IDictionary<String, TransformerState> transformersMap = config.ToDictionary(entry => entry.Name!, entry => entry.State);
            TransformerState GetTransformerState(String name) => transformersMap.TryGetValue(name, out var state) ? state : TransformerState.Off;
            return new ITransformer[]
            {
                new EmptyTransformer(output, GetTransformerState(EmptyTransformer.Name)),
                new NameOfTransformer(output, GetTransformerState(NameOfTransformer.Name)),
                new ObjectInitializerTransformer(output, GetTransformerState(ObjectInitializerTransformer.Name))
            };
        }
    }
}
