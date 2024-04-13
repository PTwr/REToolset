using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Interfaces
{
    public interface IMarshalerStore
    {
        IActivator<T>? GetActivatorFor<T>(Span<byte> data, IFluentMarshalingContext ctx);
        IDeserializator<T>? GetDeserializerFor<T>();
        ISerializator<T>? GetSerializerFor<T>();
        IDerriverableTypeMarshaler<T>? GetMarshalerToDerriveFrom<T>() where T : class;

        void RegisterRootMap<T>(T marshaller)
            where T : ITypeMarshaler, IActivator, IDeserializator, ISerializator, IDerriverableTypeMarshaler;
    }
}
