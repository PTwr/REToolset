using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using System.Runtime.CompilerServices;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    public abstract partial class BaseFieldBuilder<TDeclaringType, TMarshaledType, TBuilder, TCallbacks>
        where TBuilder : BaseFieldBuilder<TDeclaringType, TMarshaledType, TBuilder, TCallbacks>
        where TCallbacks : BaseFieldCallbacks<TDeclaringType>, new()
    {
        protected readonly ObjectBuilder<TDeclaringType> parent;
        protected readonly TCallbacks callbacks = new TCallbacks();
        private TBuilder This => (TBuilder)this;

        protected internal BaseFieldBuilder(ObjectBuilder<TDeclaringType> parent)
        {
            this.parent = parent;
        }

        public TBuilder
            WithReadMetadata(Func<TDeclaringType, object> meta)
        {
            callbacks.ReadMetadataSource.Add(meta);
            return This;
        }

        public TBuilder
            WithWriteMetadata(Func<TDeclaringType, object> meta)
        {
            callbacks.WriteMetadataSource.Add(meta);
            return This;
        }

        public abstract ObjectBuilder<TDeclaringType>
            Done();

        public TBuilder
            WithOnAfterWrite(Action<TDeclaringType, int> handler)
        {
            callbacks.OnAfterWrite = handler;
            return This;
        }

        public TBuilder
            WithAfterReadValidator(Func<TDeclaringType, bool> afterReadValidator)
        {
            callbacks.AfterReadValidator = afterReadValidator;
            return This;
        }
        public TBuilder
            WithBeforeWriteValidator(Func<TDeclaringType, bool> beforeWriteValidator)
        {
            callbacks.BeforeWriteValidator = beforeWriteValidator;
            return This;
        }

        public TBuilder
            ExecuteWhen(Func<TDeclaringType, EMarshalingType> marshalingTypeCalculator)
        {
            callbacks.MarshalingType = marshalingTypeCalculator;
            return This;
        }

        public TBuilder
            AtOffset(Func<TDeclaringType, (int offset, OffsetRelation relation)> offsetCalculator)
        {
            callbacks.OffsetCalculator = offsetCalculator;
            return This;
        }
        public TBuilder
            WithReadOrderOf(Func<TDeclaringType, int> readOrderCalculator)
        {
            callbacks.ReadOrderCalculator = readOrderCalculator;
            return This;
        }
        public TBuilder
            WithWriteOrderOf(Func<TDeclaringType, int> writeOrderCalculator)
        {
            callbacks.WriteOrderCalculator = writeOrderCalculator;
            return This;
        }
    }
}
