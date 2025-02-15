using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Collection;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Helpers;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers
{
    public class CollectionFieldMarshaler<TDeclaringType, TMarshaledType> :
        FieldMarshaler<TDeclaringType, TMarshaledType, CollectionCallbacks<TDeclaringType, TMarshaledType>>,
        IFieldMarshaler<TDeclaringType>
    {
        private readonly DefaultCollectionMarshaler collectionMarshaler;

        public CollectionFieldMarshaler(IMarshalerStore marshalerStore, CollectionCallbacks<TDeclaringType, TMarshaledType> callbacks, DefaultCollectionMarshaler collectionMarshaler)
            : base(marshalerStore, callbacks)
        {
            this.collectionMarshaler = collectionMarshaler;
        }

        public void ReadField(TDeclaringType declaringObject, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            metadata = GetFieldReadMetadata(declaringObject, metadata);

            if (callbacks.Setter is null)
                throw new Exception($"Collection Read Marshaling executed without setter method. {metadata.GetDebugInfo()}");

            if (callbacks.OffsetCalculator is null)
                throw new Exception($"Collection Read Marshaling executed without offset calculator method. {metadata.GetDebugInfo()}");

            var offset = callbacks.OffsetCalculator(declaringObject);
            offsetStack.Push(offset.offset, offset.relation);

            var result = collectionMarshaler.ListReader<TMarshaledType>(data, metadata, offsetStack, declaringObject);
            bytesRead = result.bytesRead;

            offsetStack.Pop();

            callbacks.Setter(declaringObject, result);

            callbacks.AfterReadValidator(declaringObject);
        }

        public void WriteField(TDeclaringType declaringObject, IDataBuffer data, out int bytesWrote, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            metadata = GetFieldWriteMetadata(declaringObject, metadata);

            callbacks.BeforeWriteValidator(declaringObject);

            if (callbacks.Getter is null)
                throw new Exception($"Collection Write Marshaling executed without getter method. {metadata.GetDebugInfo()}");

            if (callbacks.OffsetCalculator is null)
                throw new Exception($"Collection Write Marshaling executed without offset calculator method. {metadata.GetDebugInfo()}");

            bytesWrote = 0;
            var values = callbacks.Getter(declaringObject);
            if (values is null || !values.Any()) return;

            var offset = callbacks.OffsetCalculator(declaringObject);
            offsetStack.Push(offset.offset, offset.relation);

            collectionMarshaler.ListWriter(values, data, metadata, offsetStack, out bytesWrote);
            callbacks.OnAfterWrite(declaringObject, bytesWrote);

            offsetStack.Pop();
        }
    }
}
