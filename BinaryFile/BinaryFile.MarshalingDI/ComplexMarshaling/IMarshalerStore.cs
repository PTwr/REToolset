using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;

namespace BinaryFile.MarshalingDI.ComplexMarshaling
{
    public interface IMarshalerStore
    {
        IActivatorMarshaler<TMarshaledType> GetActivatorMarshaler<TMarshaledType>();
        IReadMarshaler<TFieldType> GetReadMarshaler<TFieldType>();
        IReadMarshaler<TFieldType> GetReadMarshaler<TFieldType>(Type actualValueType);
        IWriteMarshaler<TMarshaledType> GetWriteMarshaler<TMarshaledType>(TMarshaledType value);

        bool TryGetActivatorMarshaler<TMarshaledType>(out IActivatorMarshaler<TMarshaledType> marshaler);
        bool TryGetReadMarshaler<TMarshaledType>(out IReadMarshaler<TMarshaledType> marshaler);
        bool TryGetReadMarshaler<TMarshaledType>(Type actualValueType, out IReadMarshaler<TMarshaledType> marshaler);
        bool TryGetWriteMarshaler<TMarshaledType>(TMarshaledType value, out IWriteMarshaler<TMarshaledType> marshaler);
    }
}
