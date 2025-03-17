using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    public abstract class BaseBuilder<TDeclaringType, TBuilder, TCallbacks>
        where TBuilder : BaseBuilder<TDeclaringType, TBuilder, TCallbacks>
        where TCallbacks : BaseCallbacks<TDeclaringType>, new()
    {
        protected MarshalingFeaturesBuilder marshalingFeatures = new MarshalingFeaturesBuilder();
        protected readonly TCallbacks callbacks = new TCallbacks();
        private TBuilder This => (TBuilder)this;

        //TODO move to extensions? keep base class pure of overloads? at leats move to partials?
        public TBuilder
            WithDebugInfo(Func<TDeclaringType, string> info)
        {
            IFeature<string>.Func func = (IFeatureSet containingFeatureSet, ILifetimeScope diScope) =>
            {
                var currentObj = containingFeatureSet.GetCurentObject<TDeclaringType>();
                return info(currentObj);
            };

            WithReadWriteMetadata(func, false, EMetadataNames.DebugInfo.ToString(), int.MaxValue);
            return This;
        }
        public TBuilder
            WithDebugInfo(string info)
            => WithDebugInfo((x) => info);

        public TBuilder
            WithReadWriteMetadata<T>(IFeature<T>.Func func, bool cached, string? name = null, int maxAge = 0)
        {
            WithReadMetadata(func, cached, name, maxAge);
            WithWriteMetadata(func, cached, name, maxAge);
            return This;
        }

        public TBuilder
            WithReadMetadata<T>(IFeature<T>.Func func, bool cached, string? name = null, int maxAge = 0)
        {
            marshalingFeatures.AddReadFeature(func, cached, name, maxAge);
            return This;
        }

        public TBuilder
            WithWriteMetadata<T>(IFeature<T>.Func func, bool cached, string? name = null, int maxAge = 0)
        {
            marshalingFeatures.AddWriteFeature(func, cached, name, maxAge);
            return This;
        }
    }
}
