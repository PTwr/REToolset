using Autofac;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace BinaryFile.MarshalingDI.Context
{
    public class FeatureSet : IFeatureSet
    {
        public override string ToString()
        {
            return $"#{Generation:D3} {Name}";
        }

        private Dictionary<(Type featureType, string? featureName), List<IFeature>> featuresDict = [];
        private readonly IFeatureSet? previousGeneration;

        public FeatureSet(int generation, IFeatureSet? previousGeneration)
        {
            Generation = generation;
            this.previousGeneration = previousGeneration;
        }

        public string? Name => this.Get<string>(nameof(EMetadataNames.DebugInfo))?.GetValue();
        public int Generation { get; }

        public void AddFeature<TFeature>(IFeature feature)
        {
            if (!featuresDict.TryGetValue((typeof(TFeature), feature.Name), out var list))
            {
                featuresDict[(typeof(TFeature), feature.Name)] = list = new List<IFeature>();
            }

            list.Add(feature);
        }

        //recursively traverse feature set stack while counting recursions to limit feature effect on later generations
        public IEnumerable<IFeature<TFeature>> Traverse<TFeature>(int depth)
        {
            foreach (var feature in this.featuresDict
                .Where(x=>x.Key.featureType == typeof(TFeature))
                .SelectMany(x => x.Value)
                .OfType<IFeature<TFeature>>()
                .Reverse()
                .Where(f => f.MaxEffectiveAge >= depth))
            {
                yield return feature;
            }

            if (this.previousGeneration != null)
            {
                foreach (var feature in this.previousGeneration.Traverse<TFeature>(depth + 1))
                {
                    yield return feature;
                }
            }
        }

        public IFeature<TFeature>? Find<TFeature>(string? name = null, int depth = 0)
        {
            if (featuresDict.TryGetValue((typeof(TFeature), name), out var list))
            {
                var f = list.LastOrDefault(x => x.Name == name && x.MaxEffectiveAge >= depth);
                if (f != null)
                    return (IFeature<TFeature>)f;
            }

            if (this.previousGeneration != null)
            {
                return this.previousGeneration.Find<TFeature>(name, depth + 1);
            }

            return default;
        }

        public IEnumerable<IFeature<TFeature>> GetAll<TFeature>(string? name = null)
            => Traverse<TFeature>(0).Where(x => x.Name == name);

        public TFeature GetValue<TFeature>(TFeature fallbackValue, string? name = null)
        {
            var feature = Get<TFeature>(name);
            if (feature == null) return fallbackValue;
            return feature.GetValue();
        }
        public IFeature<TFeature>? Get<TFeature>(string? name = null)
            => Find<TFeature>(name);
        //=> GetAll<TFeature>(name).FirstOrDefault();

        public IFeature<TFeature> GetRequired<TFeature>(string? name = null)
            => Get<TFeature>(name) ?? throw new Exception($"Feature of type '{typeof(TFeature).FullName}' and name '{name}' not found in generation '{Name}' #{Generation}");
        public TFeature GetRequiredValue<TFeature>(string? name = null)
            => GetRequired<TFeature>(name).GetValue();

        public bool TryGet<TFeature>([NotNullWhen(true)] out IFeature<TFeature>? feature, string? name)
            => (feature = Get<TFeature>(name)) != null;
        public bool TryGetValue<TFeature>(out TFeature? result, string? name = null)
        {
            if (TryGet<TFeature>(out var feature, name))
            {
                result = feature.GetValue();
                return true;
            }
            result = default;
            return false;
        }
    }
}