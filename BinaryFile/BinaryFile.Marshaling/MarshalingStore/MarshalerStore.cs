using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.MarshalingStore
{
    public class MarshalerStore : IMarshalerStore
    {
        List<ITypeMarshaler> typeMarshalers = new List<ITypeMarshaler>();

        public void Register(ITypeMarshaler typeMarshaler)
        {
            typeMarshalers.Add(typeMarshaler);
        }

        public ITypeMarshaler? FindMarshaler(Type type)
        {
            return typeMarshalers.FirstOrDefault(i => i.IsFor(type));
        }

        public T? Activate<TRoot, T>(object? parent, Memory<byte> data, IMarshalingContext ctx)
            where T : TRoot
        {
            var activator = typeMarshalers
                .OfType<ITypeMarshaler<TRoot>>()
                .FirstOrDefault(i => i.IsFor(typeof(T)));
            if (activator is null) return default;

            var r = activator.Activate(parent, data, ctx);
            if (r is T) return (T)r;

            return default;
        }

        //TODO primitive types
    }
}
