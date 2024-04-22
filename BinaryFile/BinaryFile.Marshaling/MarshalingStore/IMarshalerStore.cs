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

        ITypeMarshaler<TRoot, TRoot, TDerived> Derive<TRoot, TDerived>()
            where TRoot : class
            where TDerived : class, TRoot;
        ITypeMarshaler<TRoot, TImplementation, TDerived> Derive<TRoot, TImplementation, TDerived>()
            where TImplementation : class, TRoot
            where TDerived : class, TRoot, TImplementation;

        [Obsolete("Temporary solution until recursive finder for Derivatino is done")]
        ITypeMarshaler<TRoot, TRoot, TRoot>? FindRootMarshaler<TRoot>()
            where TRoot: class;
    }
}
