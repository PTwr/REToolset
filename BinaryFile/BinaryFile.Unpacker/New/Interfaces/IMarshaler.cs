using BinaryDataHelper;

namespace BinaryFile.Unpacker.New.Interfaces
{
    public interface IMarshaler { }
    public interface IDeserializingMarshaler<in TMappedType, out TResultType>
        where TResultType : TMappedType
    {
        TResultType DeserializeInto(TMappedType mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh);
    }
    public interface ISerializingMarshaler<in TMappedType>
    {
        void SerializeFrom(TMappedType mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh);
    }
    public interface IMarshaler<in TMappedType, out TResultType> : IMarshaler, IDeserializingMarshaler<TMappedType, TResultType>, ISerializingMarshaler<TMappedType>
        where TResultType : TMappedType
    {
    }
}
