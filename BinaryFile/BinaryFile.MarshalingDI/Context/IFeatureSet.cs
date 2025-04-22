using System.Diagnostics.CodeAnalysis;

namespace BinaryFile.MarshalingDI.Context
{
    public interface IFeatureSet
    {
        int Generation { get; }
        string? Name { get; }

        void AddFeature(IFeature feature);
        void AddFeatureRange(IEnumerable<IFeature> features);

        IFeature<TFeature>? Find<TFeature>(string name, int depth);

        IFeature<TFeature>? Get<TFeature>(string? name = null);
        TFeature GetValue<TFeature>(TFeature fallbackValue, string? name = null);

        IFeature<TFeature> GetRequired<TFeature>(string? name = null);
        TFeature GetRequiredValue<TFeature>(string? name = null);

        bool TryGet<TFeature>([NotNullWhen(true)] out IFeature<TFeature>? feature, string? name);
        bool TryGetValue<TFeature>(out TFeature? result, string? name = null);

        IEnumerable<IFeature<TFeature>> Traverse<TFeature>(int depth);
        IEnumerable<IFeature<TFeature>> GetAll<TFeature>(string? name = null);
    }
}