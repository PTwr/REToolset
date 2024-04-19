using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.MarshalingStore
{
    public interface IMarshalerStore
    {
        ITypeMarshaler? FindMarshaler(Type type);
        void Register(ITypeMarshaler typeMarshaler);
    }
}
