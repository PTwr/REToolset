using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    public partial class UnaryFieldBuilder<TDeclaringType, TMarshaledType> 
        : BaseBuilder<TDeclaringType, TMarshaledType, UnaryFieldBuilder<TDeclaringType, TMarshaledType>, UnaryCallbacks<TDeclaringType, TMarshaledType>>
    {
        protected internal UnaryFieldBuilder(ObjectMarshaler<TDeclaringType>.Builder parent)
            : base(parent)
        {
        }

        public UnaryFieldBuilder<TDeclaringType, TMarshaledType>
            WithAfterReadValidator(Func<TDeclaringType, bool> afterReadValidator)
        {
            callbacks.AfterReadValidator = afterReadValidator;
            return this;
        }
        public UnaryFieldBuilder<TDeclaringType, TMarshaledType> 
            WithBeforeWriteValidator(Func<TDeclaringType, bool> beforeWriteValidator)
        {
            callbacks.BeforeWriteValidator = beforeWriteValidator;
            return this;
        }

        public ObjectMarshaler<TDeclaringType>.Builder 
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
