using BinaryFile.Marshaling.Context;
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
        ITypeMarshaler<T>? FindMarshaler<T>();
        ITypeMarshaler<T>? FindMarshaler<T>(T obj);
        void Register(ITypeMarshaler typeMarshaler);
        [Obsolete("This should not be needed")]
        T? Activate<TRoot, T>(object? parent, Memory<byte> data, IMarshalingContext ctx)
            where T : TRoot;
    }
}
