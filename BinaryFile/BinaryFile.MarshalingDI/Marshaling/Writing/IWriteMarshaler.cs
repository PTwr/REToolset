using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Writing
{
    public interface IWriteMarshaler<in TMarshaledType> : IOrderedMarshaler
    {
        void Write(TMarshaledType value, IDataBuffer data, out int bytesWrote, IMarshalingMetadata metadata, IOffsetStack offsetStack);

        bool IsForWriting(TMarshaledType value) => true;
    }
}
