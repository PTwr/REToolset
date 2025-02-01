using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Reading
{
    public interface IReadMarshaler<out TMarshaledType> : IOrderedMarshaler
    {
        TMarshaledType Read(IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack);

        bool IsForReading(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack) => true;
    }
}
