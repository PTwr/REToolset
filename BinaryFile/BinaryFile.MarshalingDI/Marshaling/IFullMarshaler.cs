using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;

namespace BinaryFile.MarshalingDI.Marshaling
{
    public interface IFullMarshaler<TMarshaledType>
        : IReadMarshaler<TMarshaledType>, IWriteMarshaler<TMarshaledType>, IActivatorMarshaler<TMarshaledType>
    { }
    public interface IFullMutableMarshaler<TMarshaledType>
        : IMutableReadMarshaler<TMarshaledType>, IWriteMarshaler<TMarshaledType>, IActivatorMarshaler<TMarshaledType>
    { }
}
