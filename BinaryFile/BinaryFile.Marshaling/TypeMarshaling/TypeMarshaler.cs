using BinaryFile.Marshaling.Activation;
using BinaryFile.Marshaling.MarshalingContext;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.TypeMarshaling
{
    public class TypeMarshaler<TRoot, TBase, TImplementation> : ITypeMarshaler<TRoot, TBase, TImplementation>
        where TBase : class, TRoot
        where TImplementation : class, TBase, TRoot
    {
        public bool IsFor(Type t)
        {
            return t.IsAssignableTo(typeof(TImplementation));
        }

        public TRoot? Activate(object? parent, Memory<byte> data, IMarshalingContext ctx, Type? type = null)
        {
            foreach(var ca in activators.OrderBy(i=>i.Order))
            {
                var r = ca.Activate(parent, data, ctx);
                if (r is not null)
                    return r;
            }

            if (type is not null)
            {
                if (!type.IsAssignableTo(typeof(TImplementation))) return default;
                foreach (var d in derivedMarshalers.Where(i=>i.IsFor(type)))
                {
                    var r = d.Activate(parent, data, ctx, type);
                    if (r is not null)
                        return r;
                }
            }

            return ActivationHelper.Activate<TImplementation>(parent);
        }

        List<ITypeMarshaler<TRoot>> derivedMarshalers = new List<ITypeMarshaler<TRoot>>();
        public ITypeMarshaler<TRoot, TImplementation, TDerived> Derive<TDerived>() where TDerived : class, TRoot, TBase, TImplementation
        {
            var x = new TypeMarshaler<TRoot, TBase, TDerived>();
            derivedMarshalers.Add(x);
            return x;
        }

        public TRoot? Deserialize(TRoot? obj, object? parent, Memory<byte> data, IMarshalingContext ctx)
        {
            if (obj is null)
            {
                obj = Activate(parent, data, ctx);
            }

            return obj;
        }


        public void Serialize(TRoot? obj)
        {
            throw new NotImplementedException();
        }

        List<ICustomActivator<TImplementation>> activators = new List<ICustomActivator<TImplementation>>();
        public ITypeMarshaler<TRoot, TBase, TImplementation> WithCustomActivator(ICustomActivator<TImplementation> customActivator)
        {
            activators.Add(customActivator);
            return this;
        }
    }
}
