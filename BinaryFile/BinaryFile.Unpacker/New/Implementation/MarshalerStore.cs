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
        HashSet<IMarshaler> primitiveMarshalers = new HashSet<IMarshaler>();

        public IDeserializingMarshaler<T, T>? GetDeserializatorFor<T>()
        {
            return GetPrimitiveDeserializer<T>() ?? (GetObjectDeserializerFor<T>() as IDeserializingMarshaler<T, T>);
        }
        public ISerializingMarshaler<T>? GetSerializatorFor<T>()
        {
            return GetPrimitiveSerializer<T>() ?? GetObjectSerializerFor<T>();
        }

        //TODO fix! This does not return derrived maps! HoldsHierarchy is for creating derivied maps not for this, reverse?
        public IActivator<T>? GetActivatorFor<T>(Span<byte> data, IMarshalingContext ctx)
        {
            foreach(var candidate in typeMarshalers
                .OfType<IActivator>()
                //.Where(i => i.HoldsHierarchyFor<T>())
                )
            {
                var result = candidate.GetActivatorFor<T>(data, ctx);
                if (result!=null) return result;
            }
            return null;
        }

        public IDeriverableTypeMarshaler<T>? GetMarshalerToDeriveFrom<T>()
            where T : class
        {
            return typeMarshalers
                .OfType<IDeriverableTypeMarshaler>()
                .Where(i => i.HoldsHierarchyFor<T>())
                .Select(i => i.GetMarshalerToDeriveFrom<T>())
                .OfType<IDeriverableTypeMarshaler<T>>()
                .FirstOrDefault();
        }

        public IDeserializator<T>? GetObjectDeserializerFor<T>()
        {
            var aa = typeMarshalers
                .OfType<IDeserializator<T>>()
                .Select(i => i.GetDeserializerFor<T>())
                .FirstOrDefault();

            return aa;
        }

        public ISerializingMarshaler<T>? GetObjectSerializerFor<T>()
        {
            return typeMarshalers
                .OfType<ISerializator<T>>()
                .Select(i => i.GetSerializerFor<T>())
                .FirstOrDefault();
        }

        public void RegisterRootMap<T>(T marshaller)
            where T : ITypeMarshaler
        {
            typeMarshalers.Add(marshaller);
        }

        public void RegisterPrimitiveMarshaler(IMarshaler marshaler)
        {
            primitiveMarshalers.Add(marshaler);
        }

        public IDeserializingMarshaler<T, T>? GetPrimitiveDeserializer<T>()
        {
            return primitiveMarshalers
                .OfType<IMarshaler<T, T>>()
                .FirstOrDefault();
        }

        public ISerializingMarshaler<T>? GetPrimitiveSerializer<T>()
        {
            return primitiveMarshalers
                .OfType<IMarshaler<T, T>>()
                .FirstOrDefault();
        }
    }
}
