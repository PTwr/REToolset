using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Reading
{
    public interface IMutableReadMarshaler<in TMarshaledType> : IOrderedMarshaler
        where TMarshaledType : class
    {
        void Read(TMarshaledType value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack);

        bool IsForMutableReading(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack) => true;
    }
}
