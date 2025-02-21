using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling
{
    public abstract class BaseMarshaler
    {
        protected readonly IDataBuffer dataBuffer;
        protected readonly IOffsetStack offsetStack;
        protected readonly IHierarchicalFeatureSet features;

        public BaseMarshaler(IDataBuffer dataBuffer, IOffsetStack offsetStack, IHierarchicalFeatureSet features)
        {
            this.dataBuffer = dataBuffer;
            this.offsetStack = offsetStack;
            this.features = features;
        }
    }
}
