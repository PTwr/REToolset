using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Reading
{
    public class LambdaReadMarshaler<TMarshaledType> : IReadMarshaler<TMarshaledType>
    {
        private readonly Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, (TMarshaledType value, int bytesRead)> reader;

        public LambdaReadMarshaler(Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, (TMarshaledType value, int bytesRead)> reader, int order)
        {
            this.reader = reader;
            Order = order;
        }

        public int Order { get; }

        public bool IsForReading(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            return true;
        }

        public TMarshaledType Read(IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            var x = reader(data, metadata, offsetStack);
            bytesRead = x.bytesRead;
            return x.value;
        }
    }
}
