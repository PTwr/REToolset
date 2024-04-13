using BinaryDataHelper;

namespace BinaryFile.Unpacker.New.Interfaces
{
    [Obsolete("Either unify with FieldMarshaler or split and unify with TypeMarshaler")]
    public interface IMarshaler<in TMappedType>
    {
        void DeserializeInto(TMappedType mappedObject, Span<byte> data, IFluentMarshalingContext ctx, out int fieldByteLengh);
        void SerializeFrom(TMappedType mappedObject, ByteBuffer data, IFluentMarshalingContext ctx, out int fieldByteLengh);
    }
}
