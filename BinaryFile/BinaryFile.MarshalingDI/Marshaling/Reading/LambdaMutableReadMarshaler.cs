using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Reading
{
    public class LambdaMutableReadMarshaler<TMarshaledType> : IMutableReadMarshaler<TMarshaledType>
        where TMarshaledType : class
    {
        private readonly Func<TMarshaledType, IDataBuffer, IMarshalingMetadata, IOffsetStack, int> reader;

        public LambdaMutableReadMarshaler(Func<TMarshaledType, IDataBuffer, IMarshalingMetadata, IOffsetStack, int> reader, int order)
        {
            this.reader = reader;
            Order = order;
        }

        public int Order { get; }

        public bool IsForMutableReading(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            return true;
        }

        public void Read(TMarshaledType value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            bytesRead = reader(value, data, metadata, offsetStack);
        }
    }
}
