using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Activating
{
    public interface IActivatorMarshaler<out TMarshaledType> : IOrderedMarshaler
    {
        TMarshaledType? Activate(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent) => default;

        bool IsForActivating(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent) => true;
    }
}
