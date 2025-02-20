using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling
{
    public abstract class BaseMarshaler
    {
        protected readonly IHierarchicalFeatureSet features;

        public BaseMarshaler(IHierarchicalFeatureSet features)
        {
            this.features = features;
        }

        protected IDataBuffer data => features.GetDataBuffer();
        protected IOffsetStack offsetStack => features.GetOffsetStack();
    }
}
