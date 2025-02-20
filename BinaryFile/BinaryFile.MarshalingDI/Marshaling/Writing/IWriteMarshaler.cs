using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Writing
{
    public interface IWriteMarshaler<in TMarshaledType> : IOrderedMarshaler
    {
        void Write(TMarshaledType value, out int bytesWrote);

        bool IsForWriting() => true;
    }
}
