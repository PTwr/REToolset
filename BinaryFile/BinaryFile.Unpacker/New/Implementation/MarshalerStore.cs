﻿using BinaryFile.Unpacker.New.Interfaces;
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

        public IActivator<T>? GetActivatorFor<T>(Span<byte> data, IMarshalingContext ctx)
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

        public IDeserializator<T>? GetObjectDeserializerFor<T>()
        {
            var aa = typeMarshalers
                .OfType<IDeserializator<T>>()
                .Select(i => i.GetDeserializerFor<T>())
                .FirstOrDefault();

            //var bb = aa as IDeserializingMarshaler<T, T>;

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

        public void RegisterPrimitiveMarshaler<T>(IMarshaler<T, T> marshaler)
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