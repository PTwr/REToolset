using Autofac;
using BinaryFile.MarshalingDI.Context;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    public class MarshalingFeaturesBuilder
    {
        public delegate IFeature FeatureBuilder(IFeatureSet containingFeatureSet, ILifetimeScope diScope);
        public List<FeatureBuilder> ReadFeatures = new();
        public List<FeatureBuilder> WriteFeatures = new();

        public void AddReadFeature<T>(IFeature<T>.Func feature, bool cached, string? name = null, int maxAge = 0)
            => ReadFeatures.Add((f, di) => new FuncFeature<T>(f, di, feature, cached, name, maxAge));
        public void AddWriteFeature<T>(IFeature<T>.Func feature, bool cached, string? name = null, int maxAge = 0)
            => WriteFeatures.Add((f, di) => new FuncFeature<T>(f, di, feature, cached, name, maxAge));

        public void ApplyReadFeatures(IFeatureSet containingFeatureSet, ILifetimeScope diScope)
            => containingFeatureSet.AddFeatureRange(ReadFeatures.Select(x => x(containingFeatureSet, diScope)));
        public void ApplyWriteFeatures(IFeatureSet containingFeatureSet, ILifetimeScope diScope)
            => containingFeatureSet.AddFeatureRange(WriteFeatures.Select(x => x(containingFeatureSet, diScope)));
    }
}
