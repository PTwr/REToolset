using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Reading
{
    public interface IMutableReadMarshaler<in TMarshaledType> : IOrderedMarshaler
    {
        void Read(TMarshaledType value, out int bytesRead);

        bool IsForMutableReading() => true;
    }
}
