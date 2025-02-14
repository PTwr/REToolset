using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    public partial class CollectionFieldBuilder<TDeclaringType, TMarshaledType>
        : BaseBuilder<TDeclaringType, TMarshaledType, CollectionFieldBuilder<TDeclaringType, TMarshaledType>, CollectionCallbacks<TDeclaringType, TMarshaledType>>
    {
        protected internal CollectionFieldBuilder(ObjectBuilder<TDeclaringType> parent)
            : base(parent)
        {
        }
    }
}
