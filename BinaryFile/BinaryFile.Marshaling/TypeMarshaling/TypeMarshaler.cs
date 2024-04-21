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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BinaryFile.Marshaling.TypeMarshaling
{
    public class RootTypeMarshaler<TRoot> : TypeMarshaler<TRoot, TRoot, TRoot>
        where TRoot : class
    {
        public RootTypeMarshaler()
        {
        }
    }
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

        public bool IsFor(Type t)
        {
            return t.IsAssignableTo(typeof(TImplementation));
        }

        public object? ActivateTypeless(object? parent, Memory<byte> data, IMarshalingContext ctx, Type? type = null)
        {
            return Activate(parent, data, ctx, type);
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

        public object? DeserializeTypeless(object? obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            fieldByteLength = 0;
            if (obj is not null && obj is not TImplementation)
                return null;

            return Deserialize((TImplementation)obj, parent, data, ctx, out fieldByteLength);
        }

        public void HandleBeforeDeserializationEvent(TImplementation obj, Memory<byte> data, IMarshalingContext ctx)
        {
            if (Parent is not null)
                Parent.HandleBeforeDeserializationEvent(obj, data, ctx);

            if (onBeforeDeserialization is not null)
                onBeforeDeserialization(obj, data, ctx);
        }
        public void HandleAfterDeserializationEvent(TImplementation obj, int byteLength, IMarshalingContext ctx)
        {
            if (Parent is not null)
                Parent.HandleAfterDeserializationEvent(obj, byteLength, ctx);

            if (onAfterDeserialization is not null)
                onAfterDeserialization(obj, byteLength, ctx);
        }

        public void HandleBeforeSerializationEvent(TImplementation obj, ByteBuffer data, IMarshalingContext ctx)
        {
            if (Parent is not null)
                Parent.HandleBeforeSerializationEvent(obj, data, ctx);

            if (onBeforeSerialization is not null)
                onBeforeSerialization(obj, data, ctx);
        }
        public void HandleAfterSerializationEvent(TImplementation obj, int byteLength, IMarshalingContext ctx)
        {
            if (Parent is not null)
                Parent.HandleAfterSerializationEvent(obj, byteLength, ctx);

            if (onAfterSerialization is not null)
                onAfterSerialization(obj, byteLength, ctx);
        }

        public TRoot? Deserialize(TRoot? obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            fieldByteLength = 0;

            if (obj is null)
                obj = Activate(parent, data, ctx);
            if (obj is not TImplementation)
                return default;

            HandleBeforeDeserializationEvent((TImplementation)obj, data, ctx);

            //allow derived marshalers to take over processing
            foreach (var dm in derivedMarshalers)
            {
                if (dm.IsFor(obj.GetType()))
                {
                    obj = dm.Deserialize(obj, parent, data, ctx, out fieldByteLength);

                    HandleAfterDeserializationEvent((TImplementation)obj, fieldByteLength, ctx);

                    return obj;
                }
            }

            //once in most-matching marshaler
            var imp = (TImplementation)obj;

            HandleBeforeDeserializationEvent(imp, data, ctx);

            //gather from parents, filter out overloaed actions, then execute in order
            foreach (var fm in DerivedMarshalingActions
                .Where(i => i.IsDeserializationEnabled)
                .OrderBy(i => i.GetDeserializationOrder(imp)))
            {
                fm.DeserializeInto(imp, data, ctx, out _);
            }

            if (byteLengthGetter is not null)
                fieldByteLength = byteLengthGetter(imp);

            HandleAfterDeserializationEvent((TImplementation)obj, fieldByteLength, ctx);

            return imp;
        }

        public void SerializeTypeless(object? obj, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            fieldByteLength = 0;
            if (obj is not null && obj is not TImplementation)
                return;

            Serialize((TImplementation)obj, data, ctx, out fieldByteLength);
        }
        public void Serialize(TRoot? obj, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            fieldByteLength = 0;

            if (obj is null)
                return;
            if (obj is not TImplementation)
                throw new Exception($"{ctx.FieldName}. Object of type {obj.GetType().Name} is not a match for {typeof(TImplementation).Name}");

            HandleBeforeSerializationEvent((TImplementation)obj, data, ctx);

            //allow derived marshalers to take over processing
            foreach (var dm in derivedMarshalers)
            {
                if (dm.IsFor(obj.GetType()))
                {
                    dm.Serialize(obj, data, ctx, out fieldByteLength);
                    return;
                }
            }

            //once in most-matching marshaler
            var imp = (TImplementation)obj;

            //gather from parents, filter out overloaed actions, then execute in order
            foreach (var fm in DerivedMarshalingActions
                .Where(i => i.IsSerializationEnabled)
                .OrderBy(i => i.GetSerializationOrder(imp)))
            {
                fm.SerializeFrom(imp, data, ctx, out _);
            }

            if (byteLengthGetter is not null)
                fieldByteLength = byteLengthGetter(imp);

            HandleAfterSerializationEvent((TImplementation)obj, fieldByteLength, ctx);
        }

        List<ICustomActivator<TImplementation>> activators = new List<ICustomActivator<TImplementation>>();
        public ITypeMarshaler<TRoot, TBase, TImplementation> WithCustomActivator(ICustomActivator<TImplementation> customActivator)
        {
            activators.Add(customActivator);
            return this;
        }

        Func<TImplementation, int>? byteLengthGetter;
        public ITypeMarshaler<TRoot, TBase, TImplementation> WithByteLengthOf(Func<TImplementation, int> getter)
        {
            byteLengthGetter = getter;
            return this;
        }
        public ITypeMarshaler<TRoot, TBase, TImplementation> WithByteLengthOf(int length) => WithByteLengthOf(i => length);

        event Action<TImplementation, Memory<byte>, IMarshalingContext>? onBeforeDeserialization = null;
        //TODO Event instead of Delegate? Or KISS and let helpers deal with multiple handlers?
        public ITypeMarshaler<TRoot, TBase, TImplementation> BeforeDeserialization(Action<TImplementation, Memory<byte>, IMarshalingContext> eventHandler)
        {
            onBeforeDeserialization += eventHandler;
            return this;
        }

        event Action<TImplementation, int, IMarshalingContext>? onAfterDeserialization = null;
        //TODO Event instead of Delegate? Or KISS and let helpers deal with multiple handlers?
        public ITypeMarshaler<TRoot, TBase, TImplementation> AfterDeserialization(Action<TImplementation, int, IMarshalingContext> eventHandler)
        {
            onAfterDeserialization += eventHandler;
            return this;
        }

        event Action<TImplementation, ByteBuffer, IMarshalingContext>? onBeforeSerialization = null;
        //TODO Event instead of Delegate? Or KISS and let helpers deal with multiple handlers?
        public ITypeMarshaler<TRoot, TBase, TImplementation> BeforeSerialization(Action<TImplementation, ByteBuffer, IMarshalingContext> eventHandler)
        {
            onBeforeSerialization += eventHandler;
            return this;
        }

        event Action<TImplementation, int, IMarshalingContext>? onAfterSerialization = null;
        //TODO Event instead of Delegate? Or KISS and let helpers deal with multiple handlers?
        public ITypeMarshaler<TRoot, TBase, TImplementation> AfterSerialization(Action<TImplementation, int, IMarshalingContext> eventHandler)
        {
            onAfterSerialization += eventHandler;
            return this;
        }
    }
}
