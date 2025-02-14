using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using System.Runtime.CompilerServices;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    public abstract partial class BaseBuilder<TDeclaringType, TMarshaledType, TBuilder, TCallbacks>
        where TBuilder : BaseBuilder<TDeclaringType, TMarshaledType, TBuilder, TCallbacks>
        where TCallbacks : BaseFieldCallbacks<TDeclaringType>, new()
    {
        protected readonly ObjectBuilder<TDeclaringType> parent;
        protected readonly TCallbacks callbacks = new TCallbacks();

        protected internal BaseBuilder(ObjectBuilder<TDeclaringType> parent)
        {
            this.parent = parent;
        }

        private TBuilder This => (TBuilder)this;
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
