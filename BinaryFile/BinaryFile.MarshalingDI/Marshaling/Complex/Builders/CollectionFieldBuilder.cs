using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.Marshaling.Collection;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    public partial class CollectionFieldBuilder<TDeclaringType, TMarshaledType>
        : BaseFieldBuilder<TDeclaringType, TMarshaledType, CollectionFieldBuilder<TDeclaringType, TMarshaledType>, CollectionCallbacks<TDeclaringType, TMarshaledType>>
    {
        public CollectionFieldBuilder(ObjectBuilder<TDeclaringType> parent)
            : base(parent)
        {
        }

        public override ObjectBuilder<TDeclaringType>
            Done()
        {
            parent.RegisterFieldMarshalerInitializer((store) =>
                new CollectionFieldMarshaler<TDeclaringType, TMarshaledType>(store, callbacks, store.Resolve<DefaultCollectionMarshaler>()));
            return parent;
        }

        public CollectionFieldBuilder<TDeclaringType, TMarshaledType>
            WithReadItemCountOf(Func<TDeclaringType, int> itemCount)
        {
            WithReadMetadata((x) => new ICollectionCountMetadata.CollectionCountMetadata(itemCount(x)));
            return this;
        }

        public CollectionFieldBuilder<TDeclaringType, TMarshaledType>
            WriteFrom(Func<TDeclaringType, IEnumerable<TMarshaledType>> getter)
        {
            callbacks.Getter = getter;
            return this;
        }

        public CollectionFieldBuilder<TDeclaringType, TMarshaledType>
            ReadInto(Action<TDeclaringType, List<(int offsetInCollection, TMarshaledType item)>> setter)
        {
            callbacks.Setter = setter;
            return this;
        }
    }
}
