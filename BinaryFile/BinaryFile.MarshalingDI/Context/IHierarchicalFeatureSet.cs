using Autofac;
using System.Diagnostics.CodeAnalysis;

namespace BinaryFile.MarshalingDI.Context
{
    public interface IHierarchicalFeatureSet
    {
        public interface IFeatureWrapper
        {
            bool IsWithinGenerationLimit(int ageLimit);
            string Name { get; }
            IFeatureWrapper BoundCopy(IContainer container);
        }
        public interface IFeatureWrapper<T> : IFeatureWrapper
        {
            T Value { get; }
        }

        void AddFeature(IFeatureWrapper feature);
        void AddFeatureRange(IEnumerable<IFeatureWrapper> features);
        void AddFuncFeature<TFeature>(Func<IContainer, TFeature> func, int maxGenerations = 0, string name = "");
        void AddValueFeature<TFeature>(TFeature feature, int maxGenerations = 0, string name = "");
        TFeature Get<TFeature>(TFeature fallbackValue, string name = "");
        TFeature GetRequired<TFeature>(string name = "");
        IEnumerable<IFeatureWrapper<TFeature>> GetAll<TFeature>(string name = "");
        void Pop();
        void Push(string name);
        bool TryGet<TFeature>([NotNullWhen(true)] out TFeature? result, string name = "");
    }
}