using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;

namespace BinaryFile.MarshalingDI.ObjectMarshaling
{
    public interface IMarshalerStore
    {
        IReadMarshaler<TFieldType> GetReadMarshaler<TFieldType, TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            where TMarshaledType : TFieldType;
        IMutableReadMarshaler<TMarshaledType> GetMutableReadMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            where TMarshaledType : class;
        IWriteMarshaler<TMarshaledType> GetWriteMarshaler<TMarshaledType>(TMarshaledType value);
        IActivatorMarshaler<TMarshaledType> GetActivatorMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent);
    }
}
