using Autofac;

namespace BinaryFile.MarshalingDI.Context
{
    public class FuncFeature<T> : IFeature<T>
    {
        public delegate T Func(IFeatureSet containingFeatureSet, ILifetimeScope diScope);

        private readonly IFeatureSet containingFeatureSet;
        private readonly ILifetimeScope diScope;
        Func func;
        private readonly bool cached;

        public FuncFeature(IFeatureSet containingFeatureSet, ILifetimeScope diScope, Func func, bool cached, string? name = null, int maxEffectiveAge = 0)
        {
            this.containingFeatureSet = (containingFeatureSet as IFeatureSetStack)?[0] ?? containingFeatureSet;
            this.diScope = diScope;
            this.func = func ?? throw new ArgumentNullException(nameof(func));
            this.cached = cached;
            this.Name = name;
            MaxEffectiveAge = maxEffectiveAge;
        }

        T? cache = default; bool cacheCalculated = false;

        public string? Name { get; }

        public int MaxEffectiveAge { get; }

        public T GetValue()
        {
            if (cached)
            {
                if (!cacheCalculated) cache = func(containingFeatureSet, diScope);
                return cache!;
            }
            return func(containingFeatureSet, diScope);

        }
    }
}