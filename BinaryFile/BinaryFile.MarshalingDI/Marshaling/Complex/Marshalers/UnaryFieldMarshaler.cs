using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Helpers;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers
{
    public partial class UnaryFieldMarshaler<TDeclaringType, TMarshaledType> :
        FieldMarshaler<TDeclaringType, TMarshaledType, UnaryCallbacks<TDeclaringType, TMarshaledType>>,
        IFieldMarshaler<TDeclaringType>
    {
        public UnaryFieldMarshaler(IMarshalerStore marshalerStore, UnaryCallbacks<TDeclaringType, TMarshaledType> callbacks)
            : base(marshalerStore, callbacks)
        {
        }

        //TODO make configurable as well and move default logic to helper? Maybe nested class as well, to be abple to receive Callbacks
        //TODO clean up processor of all non-callback scum!!!
        public void ReadField(TDeclaringType declaringObject, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            metadata = GetFieldReadMetadata(declaringObject, metadata);

            if (callbacks.Setter is null)
                throw new Exception($"Read Marshaling executed without setter method. {metadata.GetDebugInfo()}");

            if (callbacks.OffsetCalculator is null)
                throw new Exception($"Read Marshaling executed without offset calculator method. {metadata.GetDebugInfo()}");

            var offset = callbacks.OffsetCalculator(declaringObject);
            offsetStack.Push(offset.offset, offset.relation);

            var value = ReadHelper.Read<TMarshaledType>(marshalerStore, declaringObject, data, metadata, offsetStack, out bytesRead);

            offsetStack.Pop();

            callbacks.Setter(declaringObject, value);

            callbacks.AfterReadValidator(declaringObject);
        }

        public void WriteField(TDeclaringType declaringObject, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            metadata = GetFieldWriteMetadata(declaringObject, metadata);

            callbacks.BeforeWriteValidator(declaringObject);

            if (callbacks.Getter is null)
                throw new Exception($"Write Marshaling executed without getter method. {metadata.GetDebugInfo()}");

            if (callbacks.OffsetCalculator is null)
                throw new Exception($"Write Marshaling executed without offset calculator method. {metadata.GetDebugInfo()}");

            bytesRead = 0;
            var value = callbacks.Getter(declaringObject);
            if (value is null) return;

            var offset = callbacks.OffsetCalculator(declaringObject);
            offsetStack.Push(offset.offset, offset.relation);

            WriteHelper.Write(marshalerStore, value, data, metadata, offsetStack, out bytesRead);

            offsetStack.Pop();
        }
    }
}
