using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    //TODO expose as interface to remove IFieldBuilder from FluentAPI
    public abstract partial class BaseFieldBuilder<TDeclaringType, TMarshaledType, TBuilder, TCallbacks>
        : BaseBuilder<TDeclaringType, TBuilder, TCallbacks>, IFieldBuilder
        where TBuilder : BaseFieldBuilder<TDeclaringType, TMarshaledType, TBuilder, TCallbacks>
        where TCallbacks : BaseFieldCallbacks<TDeclaringType>, new()
    {
        protected readonly ObjectBuilder<TDeclaringType> parent;
        private TBuilder This => (TBuilder)this;

        protected internal BaseFieldBuilder(ObjectBuilder<TDeclaringType> parent)
        {
            this.parent = parent;
        }

        public abstract void Register(ContainerBuilder containerBuilder, Guid objectMarshalerId);

        public abstract ObjectBuilder<TDeclaringType>
            Done();

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
            WithReadWriteValidator(Func<ILifetimeScope, bool> validator)
            => this.WithBeforeWriteValidator(validator).WithAfterReadValidator(validator);

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
            AtOffset(int offset, OffsetRelation offsetRelation = OffsetRelation.Segment)
        {
            callbacks.OffsetCalculator = (ILifetimeScope c) => (offset, offsetRelation);
            return This;
        }
        public TBuilder
            AtOffset(Func<TDeclaringType, (int offset, OffsetRelation relation)> offsetCalculator)
        {
            callbacks.OffsetCalculator = (ILifetimeScope c) => offsetCalculator(c.Resolve<IFeatureSetStack>().GetParent<TDeclaringType>());
            return This;
        }
        public TBuilder
            AtOffset(Func<TDeclaringType, int> offsetCalculator, OffsetRelation relation)
        {
            callbacks.OffsetCalculator = (ILifetimeScope c) => (offsetCalculator(c.Resolve<IFeatureSetStack>().GetParent<TDeclaringType>()), relation);
            return This;
        }

        public TBuilder
            WithReadOrderOf(Func<ILifetimeScope, int> readOrderCalculator)
        {
            callbacks.ReadOrderCalculator = readOrderCalculator;
            return This;
        }
        public TBuilder
            WithReadOrderOf(Func<TDeclaringType, int> readOrderCalculator)
        {
            callbacks.ReadOrderCalculator = (ILifetimeScope c) => readOrderCalculator(c.Resolve<IFeatureSetStack>().GetParent<TDeclaringType>());
            return This;
        }
        public TBuilder
            WithReadOrderOf(int readOrder)
        {
            callbacks.ReadOrderCalculator = (c) => readOrder;
            return This;
        }

        public TBuilder
            WithWriteOrderOf(Func<ILifetimeScope, int> writeOrderCalculator)
        {
            callbacks.WriteOrderCalculator = writeOrderCalculator;
            return This;
        }
        public TBuilder
            WithWriteOrderOf(Func<TDeclaringType, int> writeOrderCalculator)
        {
            callbacks.WriteOrderCalculator = (ILifetimeScope c) => writeOrderCalculator(c.Resolve<IFeatureSetStack>().GetParent<TDeclaringType>());
            return This;
        }
        public TBuilder
            WithWriteOrderOf(int writeOrder)
        {
            callbacks.WriteOrderCalculator = (c) => writeOrder;
            return This;
        }
    }
}
