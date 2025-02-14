using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Reading
{
    public class LambdaReadMarshaler<TMarshaledType> : IReadMarshaler<TMarshaledType>
    {
        private readonly Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, (TMarshaledType value, int bytesRead)> reader;
        private readonly int order;
        private readonly Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, bool> isFor;

        public LambdaReadMarshaler(Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, (TMarshaledType value, int bytesRead)> reader, int order, Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, bool>? isFor = null)
        {
            this.reader = reader;
            this.order = order;
            this.isFor = isFor ?? ((d, m, o) => true);
        }

        public int Order(EMarshalingType marshalingType) => order;

        public bool IsForReading(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            return isFor(data, metadata, offsetStack);
        }

        public TMarshaledType Read(IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            var x = reader(data, metadata, offsetStack);
            bytesRead = x.bytesRead;
            return x.value;
        }
    }
}
