using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Interfaces
{
    public interface IMarshalerStore
    {
        IActivator<T>? GetActivatorFor<T>(Span<byte> data, IMarshalingContext ctx);
        IDeserializingMarshaler<T>? GetObjectDeserializerFor<T>();
        ISerializingMarshaler<T>? GetObjectSerializerFor<T>();
        IDerriverableTypeMarshaler<T>? GetMarshalerToDerriveFrom<T>() where T : class;

        void RegisterRootMap<T>(T marshaller)
            where T : ITypeMarshaler, IActivator, IDeserializator, ISerializator, IDerriverableTypeMarshaler;

        void RegisterPrimitiveMarshaler<T>(IMarshaler<T> marshaler);
        IDeserializingMarshaler<T>? GetPrimitiveDeserializer<T>();
        ISerializingMarshaler<T>? GetPrimitiveSerializer<T>();
        IDeserializingMarshaler<T>? GetDeserializatorFor<T>();
        ISerializingMarshaler<T>? GetSerializatorFor<T>();
    }
}
