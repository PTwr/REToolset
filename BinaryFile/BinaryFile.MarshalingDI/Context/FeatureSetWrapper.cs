using System.Diagnostics.CodeAnalysis;

namespace BinaryFile.MarshalingDI.Context
{
    public abstract class FeatureSetWrapper : IFeatureSet
    {
        public int Generation => Current.Generation;

        public string Name => Current.Name;

        protected abstract IFeatureSet Current { get; }

        public void AddFeature<TFeature>(IFeature feature)
        {
            Current.AddFeature<TFeature>(feature);
        }

        public IFeature<TFeature>? Get<TFeature>(string? name = null)
        {
            return Current.Get<TFeature>(name);
        }

        public IFeature<TFeature>? Find<TFeature>(string name, int depth)
        {
            return Current.Find<TFeature>(name, depth);
        }

        public IFeature<TFeature> GetRequired<TFeature>(string? name = null)
        {
            return Current.GetRequired<TFeature>(name);
        }

        public TFeature GetRequiredValue<TFeature>(string? name = null)
        {
            return Current.GetRequiredValue<TFeature>(name);
        }

        public TFeature GetValue<TFeature>(TFeature fallbackValue, string? name = null)
        {
            return Current.GetValue(fallbackValue, name);
        }

        public IEnumerable<IFeature<TFeature>> Traverse<TFeature>(int depth)
        {
            return Current.Traverse<TFeature>(depth);
        }

        public IEnumerable<IFeature<TFeature>> GetAll<TFeature>(string? name = null)
        {
            return Current.GetAll<TFeature>(name);
        }

        public bool TryGet<TFeature>([NotNullWhen(true)] out IFeature<TFeature>? feature, string? name)
        {
            return Current.TryGet(out feature, name);
        }

        public bool TryGetValue<TFeature>(out TFeature? result, string? name = null)
        {
            return Current.TryGetValue(out result, name);
        }
    }
}