using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Reading
{
    public interface IReadMarshaler<out TMarshaledType> : IOrderedMarshaler
    {
        TMarshaledType Read(out int bytesRead);

        bool IsForReading() => true;
    }
}
