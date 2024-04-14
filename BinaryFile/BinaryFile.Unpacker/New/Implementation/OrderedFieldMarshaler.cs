using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker.New.Interfaces;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Implementation
{
    //TODO separate Marshaling interface from Configuration interface
    //TODO when writing Config having marshaling fields/methods popup is annoying
    //TODO and then expose Unary/Collection Marshalers via interface instead of concrete classes
    public abstract class OrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TImplementation> : IOrderedFieldMarshaler<TDeclaringType>
        where TImplementation : OrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TImplementation>
        where TDeclaringType: class //TODO check if its needed to load TypeMarshalers?
    {
        protected OrderedFieldMarshaler(string name)
        {
            //TODO check for name conflicts when registering
            //TODO allow child to overwrite parent field map? (either to change behavior or to disable, which is same as changing behaviour :D)
            //TODO override should be explicitly stated, no implicit bullshit here!
            //TODO maybe some `bool doNotExecuteConflictingBaseMaps` and filter out overriden maps when gathering maps from hierarchy?
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Provide field name or you will suffer horribly during debuging!");

            Name = name;
        }

        public bool IsDeserializationEnabled { get; protected set; } = false;
        public bool IsSerializationEnabled { get; protected set; } = false;

        public int GetOrder(TDeclaringType mappedType) =>
            orderGetter?.Invoke(mappedType) ?? 0;
        public int GetSerializationOrder(TDeclaringType mappedType) =>
            serializationOrderGetter?.Invoke(mappedType) ?? orderGetter?.Invoke(mappedType) ?? 0;
        public int GetDeserializationOrder(TDeclaringType mappedType) =>
            deserializationOrderGetter?.Invoke(mappedType) ?? orderGetter?.Invoke(mappedType) ?? 0;

        public string Name { get; }

        public abstract void DeserializeInto(TDeclaringType mappedObject, Span<byte> data, Interfaces.IMarshalingContext ctx, out int fieldByteLengh);

        public abstract void SerializeFrom(TDeclaringType mappedObject, ByteBuffer data, Interfaces.IMarshalingContext ctx, out int fieldByteLengh);

        //hmmm! Order has to either be func or use disgusting ctx containing object
        protected Func<TDeclaringType, int>? orderGetter;
        protected Func<TDeclaringType, int>? deserializationOrderGetter;
        protected Func<TDeclaringType, int>? serializationOrderGetter;

        public TImplementation WithOrderOf(Func<TDeclaringType, int> orderGetter)
        {
            this.orderGetter = orderGetter;
            return (TImplementation)this;
        }
        public TImplementation WithOrderOf(int order) => WithOrderOf(i => order);
        public TImplementation WithDeserializationOrderOf(Func<TDeclaringType, int> orderGetter)
        {
            this.deserializationOrderGetter = orderGetter;
            return (TImplementation)this;
        }
        public TImplementation WithDeserializationOrderOf(int order) => WithDeserializationOrderOf(i => order);
        public TImplementation WithSerializationOrderOf(Func<TDeclaringType, int> orderGetter)
        {
            this.serializationOrderGetter = orderGetter;
            return (TImplementation)this;
        }
        public TImplementation WithSerializationOrderOf(int order) => WithSerializationOrderOf(i => order);

        protected Func<TDeclaringType, int>? offsetGetter;
        //TODO contemplate, do such simple Func/Action need to be put into delegates?
        public TImplementation AtOffset(Func<TDeclaringType, int> offsetGetter)
        {
            this.offsetGetter = offsetGetter;
            return (TImplementation)this;
        }
        //TODO contemplate if FundField was good idea, it struggled with more params
        public TImplementation AtOffset(int offset) => AtOffset(i => offset);

        protected Func<TDeclaringType, OffsetRelation>? offsetRelationGetter;
        public TImplementation RelativeTo(Func<TDeclaringType, OffsetRelation> offsetRelationGetter)
        {
            this.offsetRelationGetter = offsetRelationGetter;
            return (TImplementation)this;
        }
        public TImplementation RelativeTo(OffsetRelation offsetRelation) => RelativeTo(i => offsetRelation);

        protected Func<TDeclaringType, int>? lengthGetter;
        public TImplementation WithByteLengthOf(Func<TDeclaringType, int> lengthGetter)
        {
            this.lengthGetter = lengthGetter;
            return (TImplementation)this;
        }
        public TImplementation WithByteLengthOf(int length) => WithByteLengthOf(i => length);

        public delegate TMarshaledType MarshalingValueGetter(TDeclaringType declaringObject, TFieldType item);
        public delegate TFieldType MarshalingValueSetter(TDeclaringType declaringObject, TFieldType item, TMarshaledType marshaledValue);
        protected MarshalingValueGetter? marshalingValueGetter;
        protected MarshalingValueSetter? marshalingValueSetter;
        public TImplementation MarshalFrom(MarshalingValueGetter getter)
        {
            marshalingValueGetter = getter;
            return (TImplementation)this;
        }
        public TImplementation MarshalInto(MarshalingValueSetter setter)
        {
            marshalingValueSetter = setter;
            return (TImplementation)this;
        }

        //TODO add rest of meta fields
        //TODO implement Unary and test
    }
}
