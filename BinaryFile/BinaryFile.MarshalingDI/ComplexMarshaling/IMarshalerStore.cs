using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;

namespace BinaryFile.MarshalingDI.ComplexMarshaling
{
    public interface IMarshalerStore
    {
        bool TryGetReadMarshaler<TMarshaledType>(out IReadMarshaler<TMarshaledType> marshaler);
        bool TryGetReadMarshaler<TFieldType, TMarshaledType>(out IReadMarshaler<TFieldType> marshaler)
            where TMarshaledType : TFieldType;
        bool TryGetMutableReadMarshaler<TMarshaledType>(out IMutableReadMarshaler<TMarshaledType> marshaler);
        bool TryGetMutableReadMarshaler<TMarshaledType>(Type valueType, out IMutableReadMarshaler<TMarshaledType> marshaler);
        bool TryGetWriteMarshaler<TMarshaledType>(TMarshaledType value, out IWriteMarshaler<TMarshaledType> marshaler);
        bool TryGetActivatorMarshaler<TMarshaledType>(out IActivatorMarshaler<TMarshaledType> marshaler);

        IReadMarshaler<TMarshaledType> GetReadMarshaler<TMarshaledType>();
        IReadMarshaler<TFieldType> GetReadMarshaler<TFieldType, TMarshaledType>()
            where TMarshaledType : TFieldType;
        IMutableReadMarshaler<TMarshaledType> GetMutableReadMarshaler<TMarshaledType>()
            where TMarshaledType : class;
        IWriteMarshaler<TMarshaledType> GetWriteMarshaler<TMarshaledType>(TMarshaledType value);
        IActivatorMarshaler<TMarshaledType> GetActivatorMarshaler<TMarshaledType>();
    }
}
