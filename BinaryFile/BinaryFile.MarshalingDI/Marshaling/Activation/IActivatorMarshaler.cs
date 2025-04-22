using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Activating
{
    public interface IActivatorMarshaler : IOrderedMarshaler
    {
        bool IsForActivating() => true;
    }
    public interface IActivatorMarshaler<out TMarshaledType> : IActivatorMarshaler
    {
        TMarshaledType? Activate() => default;
    }
}
