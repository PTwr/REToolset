using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    public class CollectionFieldMarshaler<TDeclaringType, TMarshaledType> :
        FieldMarshaler<TDeclaringType, TMarshaledType, CollectionCallbacks<TDeclaringType, TMarshaledType>>,
        IFieldMarshaler<TDeclaringType>
    {
        public CollectionFieldMarshaler(IMarshalerStore marshalerStore, CollectionCallbacks<TDeclaringType, TMarshaledType> callbacks)
            : base(marshalerStore, callbacks)
        {
        }

        public bool IsForReading(TDeclaringType declaringObject)
        {
            throw new NotImplementedException();
        }

        public bool IsForWriting(TDeclaringType declaringObject)
        {
            throw new NotImplementedException();
        }

        public void ReadField(TDeclaringType declaringObject, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            throw new NotImplementedException();
        }

        public int ReadOrder(TDeclaringType declaringObject)
        {
            throw new NotImplementedException();
        }

        public void WriteField(TDeclaringType declaringObject, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            throw new NotImplementedException();
        }

        public int WriteOrder(TDeclaringType declaringObject)
        {
            throw new NotImplementedException();
        }
    }
}
