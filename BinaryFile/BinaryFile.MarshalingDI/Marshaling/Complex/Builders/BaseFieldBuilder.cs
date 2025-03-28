using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    public abstract partial class BaseFieldBuilder<TDeclaringType, TMarshaledType, TBuilder, TCallbacks>
        : BaseBuilder<TDeclaringType, TBuilder, TCallbacks>
        where TBuilder : BaseFieldBuilder<TDeclaringType, TMarshaledType, TBuilder, TCallbacks>
        where TCallbacks : BaseFieldCallbacks<TDeclaringType>, new()
    {
        protected readonly ObjectBuilder<TDeclaringType> parent;
        private TBuilder This => (TBuilder)this;

        protected internal BaseFieldBuilder(ObjectBuilder<TDeclaringType> parent)
        {
            this.parent = parent;
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
