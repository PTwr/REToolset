using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;

namespace BinaryFile.MarshalingDI.ComplexMarshaling
{
    public interface IMarshalerStore
    {
        bool TryGetReadMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out IReadMarshaler<TMarshaledType> marshaler);
        bool TryGetReadMarshaler<TFieldType, TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out IReadMarshaler<TFieldType> marshaler)
            where TMarshaledType : TFieldType;
        bool TryGetMutableReadMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out IMutableReadMarshaler<TMarshaledType> marshaler);
        bool TryGetMutableReadMarshaler<TMarshaledType>(Type valueType, IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out IMutableReadMarshaler<TMarshaledType> marshaler);
        bool TryGetWriteMarshaler<TMarshaledType>(TMarshaledType value, out IWriteMarshaler<TMarshaledType> marshaler);
        bool TryGetActivatorMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent, out IActivatorMarshaler<TMarshaledType> marshaler);

        IReadMarshaler<TMarshaledType> GetReadMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack);
        IReadMarshaler<TFieldType> GetReadMarshaler<TFieldType, TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            where TMarshaledType : TFieldType;
        IMutableReadMarshaler<TMarshaledType> GetMutableReadMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            where TMarshaledType : class;
        IWriteMarshaler<TMarshaledType> GetWriteMarshaler<TMarshaledType>(TMarshaledType value);
        IActivatorMarshaler<TMarshaledType> GetActivatorMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent);

        T Resolve<T>()
            where T : notnull;
    }
}
