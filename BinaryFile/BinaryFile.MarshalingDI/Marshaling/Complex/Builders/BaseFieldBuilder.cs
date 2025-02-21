using Autofac;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using System.Runtime.CompilerServices;
using static BinaryFile.MarshalingDI.Context.IHierarchicalFeatureSet;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    public abstract partial class BaseFieldBuilder<TDeclaringType, TMarshaledType, TBuilder, TCallbacks>
        where TBuilder : BaseFieldBuilder<TDeclaringType, TMarshaledType, TBuilder, TCallbacks>
        where TCallbacks : BaseFieldCallbacks<TDeclaringType>, new()
    {
        protected MarshalingFeatures MarshalingFeatures = new MarshalingFeatures();
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
            var func = (ILifetimeScope c) => info(c
                .Resolve<IHierarchicalFeatureSet>()
                .GetRequired<TDeclaringType>(EMetadataNames.ParentObject.ToString()));
            var feature = new HierarchicalFeatureSet.FuncFeatureWrapper<string>(func, int.MaxValue, EMetadataNames.DebugInfo.ToString(), cached: true);

            return this.WithReadWriteMetadata(feature);
        }

        public TBuilder
            WithReadWriteMetadata(IFeatureWrapper feature)
        {
            WithReadMetadata(feature);
            WithWriteMetadata(feature);
            return This;
        }

        public TBuilder
            WithReadMetadata(IFeatureWrapper feature)
        {
            MarshalingFeatures.ReadFeatures.Add(feature);
            return This;
        }

        public TBuilder
            WithWriteMetadata(IFeatureWrapper feature)
        {
            MarshalingFeatures.WriteFeatures.Add(feature);
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
