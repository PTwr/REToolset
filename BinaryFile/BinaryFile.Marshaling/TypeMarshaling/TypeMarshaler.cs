using BinaryFile.Marshaling.Activation;
using BinaryFile.Marshaling.FieldMarshaling;
using BinaryFile.Marshaling.Context;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryDataHelper;

namespace BinaryFile.Marshaling.TypeMarshaling
{
    public partial class TypeMarshaler<TRoot, TBase, TImplementation>
        : ITypeMarshaler<TRoot, TBase, TImplementation>, ITypelessMarshaler
        where TBase : class, TRoot
        where TImplementation : class, TBase, TRoot
    {
        ITypeMarshaler<TRoot, TBase>? Parent;
        public TypeMarshaler()
        {
            
        }
        public TypeMarshaler(ITypeMarshaler<TRoot, TBase> parent)
        {
            Parent = parent;
        }

        public object? ActivateTypeless(object? parent, Memory<byte> data, IMarshalingContext ctx, Type? type = null)
        {
            return Activate(parent, data, ctx, type);
        }
        public object? DeserializeTypeless(object? obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            fieldByteLength = 0;
            if (obj is not null && obj is not TRoot)
                return null;

            return Deserialize((TRoot)obj, parent, data, ctx, out fieldByteLength);
        }

        public bool IsFor(Type t)
        {
            return t.IsAssignableTo(typeof(TImplementation));
        }

        public TRoot? Activate(object? parent, Memory<byte> data, IMarshalingContext ctx, Type? type = null)
        {
            foreach (var ca in activators.OrderBy(i => i.Order))
            {
                var r = ca.Activate(parent, data, ctx);
                if (r is not null)
                    return r;
            }

            if (type is not null)
            {
                if (!type.IsAssignableTo(typeof(TImplementation))) return default;
                foreach (var d in derivedMarshalers.Where(i => i.IsFor(type)))
                {
                    var r = d.Activate(parent, data, ctx, type);
                    if (r is not null)
                        return r;
                }
            }

            return ActivationHelper.Activate<TImplementation>(parent);
        }
        
        List<ITypeMarshalerWithActivation<TRoot>> derivedMarshalers = new List<ITypeMarshalerWithActivation<TRoot>>();
        public ITypeMarshaler<TRoot, TImplementation, TDerived> Derive<TDerived>() where TDerived : class, TRoot, TBase, TImplementation
        {
            var x = new TypeMarshaler<TRoot, TImplementation, TDerived>(this);
            derivedMarshalers.Add(x);
            return x;
        }

        public TRoot? Deserialize(TRoot? obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            fieldByteLength = 0;

            if (obj is null)
                obj = Activate(parent, data, ctx);
            if (obj is not TImplementation)
                return default;

            //allow derived marshalers to take over processing
            foreach (var dm in derivedMarshalers)
            {
                if (dm.IsFor(obj.GetType()))
                {
                    obj = dm.Deserialize(obj, parent, data, ctx, out fieldByteLength);
                    return obj;
                }
            }

            //once in most-matching marshaler
            var imp = (TImplementation)obj;

            //gather from parents, filter out overloaed actions, then execute in order
            foreach (var fm in DerivedMarshalingActions
                .Where(i => i.IsDeserializationEnabled)
                .OrderBy(i => i.GetDeserializationOrder(imp)))
            {
                fm.DeserializeInto(imp, data, ctx, out _);
            }

            return imp;
        }

        public object? SerializeTypeless(object? obj, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            throw new NotImplementedException();
        }
        public void Serialize(TRoot? obj, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
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
