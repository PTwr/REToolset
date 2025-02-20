using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Activating
{
    public interface IActivatorMarshaler<out TMarshaledType> : IOrderedMarshaler
    {
        TMarshaledType? Activate() => default;

        bool IsForActivating() => true;
    }
}
