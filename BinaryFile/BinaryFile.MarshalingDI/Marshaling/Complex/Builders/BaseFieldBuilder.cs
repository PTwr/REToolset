using Autofac;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using System.Runtime.CompilerServices;
using static BinaryFile.MarshalingDI.Context.IFeatureSetStack;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    public abstract partial class BaseFieldBuilder<TDeclaringType, TMarshaledType, TBuilder, TCallbacks>
        where TBuilder : BaseFieldBuilder<TDeclaringType, TMarshaledType, TBuilder, TCallbacks>
        where TCallbacks : BaseFieldCallbacks<TDeclaringType>, new()
    {
        protected MarshalingFeaturesBuilder marshalingFeatures = new MarshalingFeaturesBuilder();
        protected readonly ObjectBuilder<TDeclaringType> parent;
        protected readonly TCallbacks callbacks = new TCallbacks();
        private TBuilder This => (TBuilder)this;

        protected internal BaseFieldBuilder(ObjectBuilder<TDeclaringType> parent)
        {
            this.parent = parent;
        }

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

        public abstract ObjectBuilder<TDeclaringType>
            Done(ContainerBuilder containerBuilder);

        public TBuilder
            WithOnAfterWrite(Action<ILifetimeScope, int> handler)
        {
            callbacks.OnAfterWrite = handler;
            return This;
        }

        public TBuilder
            WithAfterReadValidator(Func<ILifetimeScope, bool> afterReadValidator)
        {
            callbacks.AfterReadValidator = afterReadValidator;
            return This;
        }
        public TBuilder
            WithBeforeWriteValidator(Func<ILifetimeScope, bool> beforeWriteValidator)
        {
            callbacks.BeforeWriteValidator = beforeWriteValidator;
            return This;
        }

        public TBuilder
            ExecuteWhen(Func<ILifetimeScope, EMarshalingType> marshalingTypeCalculator)
        {
            callbacks.MarshalingType = marshalingTypeCalculator;
            return This;
        }

        public TBuilder
            AtOffset(Func<ILifetimeScope, (int offset, OffsetRelation relation)> offsetCalculator)
        {
            callbacks.OffsetCalculator = offsetCalculator;
            return This;
        }
        public TBuilder
            WithReadOrderOf(Func<ILifetimeScope, int> readOrderCalculator)
        {
            callbacks.ReadOrderCalculator = readOrderCalculator;
            return This;
        }
        public TBuilder
            WithWriteOrderOf(Func<ILifetimeScope, int> writeOrderCalculator)
        {
            callbacks.WriteOrderCalculator = writeOrderCalculator;
            return This;
        }
    }
}
