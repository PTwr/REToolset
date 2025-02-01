using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Writing
{
    public class LambdaWriterMarshaler<TMarshaledType> : IWriteMarshaler<TMarshaledType>
    {
        private readonly Func<TMarshaledType, IDataBuffer, IMarshalingMetadata, IOffsetStack, int> writer;
        public int Order { get; }

        public LambdaWriterMarshaler(Func<TMarshaledType, IDataBuffer, IMarshalingMetadata, IOffsetStack, int> writer, int order)
        {
            this.writer = writer;
            Order = order;
        }

        public void Write(TMarshaledType value, IDataBuffer data, out int bytesWrote, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            bytesWrote = writer(value, data, metadata, offsetStack);
        }
    }
}
