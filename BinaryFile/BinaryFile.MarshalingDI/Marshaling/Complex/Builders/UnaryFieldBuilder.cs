using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    public partial class UnaryFieldBuilder<TDeclaringType, TMarshaledType>
        : BaseFieldBuilder<TDeclaringType, TMarshaledType, UnaryFieldBuilder<TDeclaringType, TMarshaledType>, UnaryCallbacks<TDeclaringType, TMarshaledType>>
    {
        protected internal UnaryFieldBuilder(ObjectBuilder<TDeclaringType> parent)
            : base(parent)
        {
        }

        public override ObjectBuilder<TDeclaringType>
            Done()
        {
            parent.RegisterFieldMarshalerInitializer((store) =>
                new UnaryFieldMarshaler<TDeclaringType, TMarshaledType>(store, callbacks));
            return parent;
        }

        public UnaryFieldBuilder<TDeclaringType, TMarshaledType>
            WriteFrom(Func<TDeclaringType, TMarshaledType?> getter)
        {
            callbacks.Getter = getter;
            return this;
        }
        public UnaryFieldBuilder<TDeclaringType, TMarshaledType>
            ReadInto(Action<TDeclaringType, TMarshaledType?> setter)
        {
            callbacks.Setter = setter;
            return this;
        }
    }
}
