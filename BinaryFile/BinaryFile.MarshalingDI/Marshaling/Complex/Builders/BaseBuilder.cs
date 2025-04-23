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
            WithReadWriteMetadata<T>(T value, string? name = null, int maxAge = 0)
        {
            IFeature<T>.Func func = (x, y) => value;
            WithReadMetadata(func, true, name, maxAge);
            WithWriteMetadata(func, true, name, maxAge);
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

        /// <summary>
        /// BigEndian - human readable hexes, Little Endian - Intels annoying memory layout
        /// </summary>
        /// <returns></returns>
        public TBuilder
            InLittleEndian()
            => this.WithReadWriteMetadata((f, s) => EMarshalingEndianness.LittleEndian, true, null, int.MaxValue);
        /// <summary>
        /// BigEndian - human readable hexes, Little Endian - Intels annoying memory layout
        /// </summary>
        /// <returns></returns>
        public TBuilder
            InBigEndian()
            => this.WithReadWriteMetadata((f, s) => EMarshalingEndianness.BigEndian, true, null, int.MaxValue);
    }
}
