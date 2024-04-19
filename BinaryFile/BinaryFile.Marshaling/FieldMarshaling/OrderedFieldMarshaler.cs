using BinaryDataHelper;
using BinaryFile.Marshaling.Common;
using BinaryFile.Marshaling.MarshalingContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.FieldMarshaling
{
    //TODO separate Marshaling interface from Configuration interface
    //TODO when writing Config having marshaling fields/methods popup is annoying
    //TODO and then expose Unary/Collection Marshalers via interface instead of concrete classes
    public abstract class OrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
        : IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
        where TInterface : class, IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
        where TDeclaringType : class
    {
        private TInterface This => (TInterface)(IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>)this;

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

        public abstract void DeserializeInto(TDeclaringType mappedObject, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLengh);

        public abstract void SerializeFrom(TDeclaringType mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh);

        //hmmm! Order has to either be func or use disgusting ctx containing object
        protected Func<TDeclaringType, int>? orderGetter;
        protected Func<TDeclaringType, int>? deserializationOrderGetter;
        protected Func<TDeclaringType, int>? serializationOrderGetter;

        public TInterface WithOrderOf(Func<TDeclaringType, int> orderGetter)
        {

            this.orderGetter = orderGetter;
            return This;
        }
        public TInterface WithOrderOf(int order) => WithOrderOf(i => order);
        public TInterface WithDeserializationOrderOf(Func<TDeclaringType, int> orderGetter)
        {
            deserializationOrderGetter = orderGetter;
            return This;
        }
        public TInterface WithDeserializationOrderOf(int order) => WithDeserializationOrderOf(i => order);
        public TInterface WithSerializationOrderOf(Func<TDeclaringType, int> orderGetter)
        {
            serializationOrderGetter = orderGetter;
            return This;
        }
        public TInterface WithSerializationOrderOf(int order) => WithSerializationOrderOf(i => order);

        protected Func<TDeclaringType, int>? offsetGetter;
        //TODO contemplate, do such simple Func/Action need to be put into delegates?
        public TInterface AtOffset(Func<TDeclaringType, int> offsetGetter)
        {
            this.offsetGetter = offsetGetter;
            return This;
        }
        //TODO contemplate if FundField was good idea, it struggled with more params
        public TInterface AtOffset(int offset) => AtOffset(i => offset);

        protected Func<TDeclaringType, OffsetRelation>? offsetRelationGetter;
        public TInterface RelativeTo(Func<TDeclaringType, OffsetRelation> offsetRelationGetter)
        {
            this.offsetRelationGetter = offsetRelationGetter;
            return This;
        }
        public TInterface RelativeTo(OffsetRelation offsetRelation) => RelativeTo(i => offsetRelation);

        protected Func<TDeclaringType, int>? lengthGetter;
        public TInterface WithByteLengthOf(Func<TDeclaringType, int> lengthGetter)
        {
            this.lengthGetter = lengthGetter;
            return This;
        }
        public TInterface WithByteLengthOf(int length) => WithByteLengthOf(i => length);

        protected IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
            .MarshalingValueGetter? marshalingValueGetter;
        protected IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
            .MarshalingValueSetter? marshalingValueSetter;
        public TInterface MarshalFrom(IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>.MarshalingValueGetter getter)
        {
            marshalingValueGetter = getter;
            return This;
        }
        public TInterface MarshalInto(IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>.MarshalingValueSetter setter)
        {
            marshalingValueSetter = setter;
            return This;
        }

        protected Func<TDeclaringType, Encoding>? encodingGetter;
        public TInterface WithEncoding(Func<TDeclaringType, Encoding> getter)
        {
            encodingGetter = getter;
            return This;
        }

        public TInterface WithEncoding(Encoding encoding) => WithEncoding(i => encoding);

        protected Func<TDeclaringType, bool>? nullTermiantionGetter;
        public TInterface WithNullTerminator(Func<TDeclaringType, bool> getter)
        {
            nullTermiantionGetter = getter;
            return This;
        }

        public TInterface WithNullTerminator(bool isNullTerminated = true) => WithNullTerminator(i => isNullTerminated);

        protected Func<TDeclaringType, bool>? littleEndianGetter;
        public TInterface AsLittleendian(Func<TDeclaringType, bool> getter)
        {
            littleEndianGetter = getter;
            return This;
        }

        public TInterface AsLittleendian(bool isNullTerminated) => WithNullTerminator(i => isNullTerminated);

        protected IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
            .AfterSerializingEvent? afterSerializingEvent;
        public TInterface AfterSerializing(IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>.AfterSerializingEvent hook)
        {
            afterSerializingEvent = hook;
            return This;
        }

        protected List<IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
            .CustomActivatorEvent> customActivatorEvents = new List<IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>.CustomActivatorEvent>();
        public TInterface WithCustomActivator(IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>.CustomActivatorEvent activator)
        {
            customActivatorEvents.Add(activator);
            return This;
        }
        protected TFieldType? CustomActivation(TDeclaringType parent, Memory<byte> data, IMarshalingContext ctx)
        {
            foreach (var activator in customActivatorEvents)
            {
                var result = activator(parent, data, ctx);
                if (result is not null)
                    return result;
            }
            return default;
        }

        //TODO add rest of meta fields
        //TODO implement Unary and test
    }
}
