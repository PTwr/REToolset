using BinaryFile.Unpacker.New.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Implementation
{
    public class MarshalerStore : IMarshalerStore
    {
        List<ITypeMarshaler> typeMarshalers = new List<ITypeMarshaler>();

        public IActivator<T>? GetActivatorFor<T>(Span<byte> data, IFluentMarshalingContext ctx)
        {
            foreach(var candidate in typeMarshalers
                .OfType<IActivator>()
                .Where(i => i.HoldsHierarchyFor<T>()))
            {
                var result = candidate.GetActivatorFor<T>(data, ctx);
                if (result!=null) return result;
            }
            return null;
        }

        public IDerriverableTypeMarshaler<T>? GetMarshalerToDerriveFrom<T>()
            where T : class
        {
            return typeMarshalers
                .OfType<IDerriverableTypeMarshaler>()
                .Where(i => i.HoldsHierarchyFor<T>())
                .Select(i => i.GetMarshalerToDerriveFrom<T>())
                .OfType<IDerriverableTypeMarshaler<T>>()
                .FirstOrDefault();
        }

        public IDeserializator<T>? GetDeserializerFor<T>()
        {
            return typeMarshalers
                .OfType<IDeserializator<T>>()
                .Select(i => i.GetDeserializerFor<T>())
                .FirstOrDefault();
        }

        public ISerializator<T>? GetSerializerFor<T>()
        {
            return typeMarshalers
                .OfType<ISerializator<T>>()
                .Select(i => i.GetSerializerFor<T>())
                .FirstOrDefault();
        }

        public void RegisterRootMap<T>(T marshaller)
            where T : ITypeMarshaler, IActivator, IDeserializator, ISerializator, IDerriverableTypeMarshaler
        {
            typeMarshalers.Add(marshaller);
        }
    }
}
