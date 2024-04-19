using BinaryFile.Marshaling.Activation;
using BinaryFile.Marshaling.MarshalingContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.TypeMarshaling
{
    public interface ITypeMarshaler
    {
        bool IsFor(Type t);
    }
    public interface ITypeMarshaler<TRoot> : ITypeMarshaler
    {
        TRoot? Activate(object? parent, Memory<byte> data, IMarshalingContext ctx, Type? type = null);
        TRoot? Deserialize(TRoot? obj, object? parent, Memory<byte> data, IMarshalingContext ctx);
        void Serialize(TRoot? obj);
    }
    public interface ITypeMarshaler<TRoot, in TBase, in TImplementation> : ITypeMarshaler<TRoot>
        where TBase : class, TRoot
        where TImplementation : class, TBase, TRoot
    {
        ITypeMarshaler<TRoot, TImplementation, TDerived> Derive<TDerived>()
            where TDerived : class, TRoot, TBase, TImplementation;

        ITypeMarshaler<TRoot, TBase, TImplementation> WithCustomActivator(ICustomActivator<TImplementation> customActivator);
    }
}
