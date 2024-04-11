using BinaryDataHelper;
using BinaryFile.Unpacker.New.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Implementation
{
    public interface IHasFieldMarshalers<in TImplementation>
    {
        IEnumerable<IOrderedFieldMarshaler<TImplementation>> OrderedFieldMarshalers { get; }
    }

    public interface ITypeMarshaler<TMappedType> : IMarshaler<TMappedType>
    {
        bool IsFor(Type type);
        TMappedType Activate(Type type, object? parent = null);
    }
    //TODO split config and useage interfaces
    public class TypeMarshaler<TBase, TImplementation> : ITypeMarshaler<TBase>, IHasFieldMarshalers<TImplementation>
        where TImplementation : class, TBase
        where TBase : class
    {
        List<ITypeMarshaler<TImplementation>> Derrived = new List<ITypeMarshaler<TImplementation>>();

        List<IOrderedFieldMarshaler<TImplementation>> marshalers = new List<IOrderedFieldMarshaler<TImplementation>>();
        public IEnumerable<IOrderedFieldMarshaler<TImplementation>> OrderedFieldMarshalers =>
            (parent == null ? marshalers : marshalers.Concat(parent.OrderedFieldMarshalers));

        private IHasFieldMarshalers<TImplementation> parent;

        //For child Implementation becomes Base
        public TypeMarshaler(IHasFieldMarshalers<TBase> parent)
        {
            this.parent = parent;
        }

        //TODO rethink can this be used for both activation and marshaling?!
        //TODO rethink Assignable To/From for deserialization/serialization/marshaling scenarios
        //TODO gotta do the covariant/contravariant bullshit :D
        public bool IsFor(Type type)
        {
            return type.IsAssignableTo(typeof(TImplementation));
        }

        public TBase Activate(Type type, object? parent = null)
        {
            //recursively attempt activation
            foreach (var d in Derrived)
            {
                if (d.IsFor(type)) return d.Activate(type);
            }

            //until there are no derrived classes or no derrived Marshaler reports as being a fit)
            if (parent is not null)
            {
                var ctor = typeof(TImplementation).GetConstructor([parent.GetType()]);
                if (ctor is not null) return (TImplementation)ctor.Invoke([parent]);
            }
            return Activator.CreateInstance<TImplementation>();
        }

        public void Deserialize(TBase mappedObject, IFluentMarshalingContext ctx, Span<byte> data, out int fieldByteLengh)
        {
            //TODO port events from old implementation

            fieldByteLengh = 0;

            //TODO nice exception for situation that shoudl not occur
            if (mappedObject is not TImplementation) throw new Exception("SHIT!");
            var implementationObject = (TImplementation)mappedObject;

            //recurse!
            foreach (var d in Derrived)
            {
                //Only one derrived class supported! No stupid multiinheritance here :)
                if (d.IsFor(implementationObject.GetType()))
                {
                    d.Deserialize(implementationObject, ctx, data, out fieldByteLengh);
                    return;
                }
            }

            //until there are no derrived classes or no derrived Marshaler reports as being a fit
            foreach (var marshaler in this.OrderedFieldMarshalers
                .Where(i => i.IsDeserializationEnabled)
                .OrderBy(i => i.GetDeserializationOrder(implementationObject)))
            {
                marshaler.Deserialize(implementationObject, ctx, data, out var l);
                fieldByteLengh += l;
            }

            //TODO TypeMarshaler-level GetLength to fill fieldByteLength without fucking around every property using this type
        }

        public void Serialize(TBase mappedObject, IFluentMarshalingContext ctx, ByteBuffer data, out int fieldByteLengh)
        {
            throw new NotImplementedException();
        }
    }
}
