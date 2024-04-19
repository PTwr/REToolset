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

        //TODO primitive types
    }
}
