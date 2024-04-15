using BinaryDataHelper;
using BinaryFile.Unpacker.Marshalers.Fluent;
using BinaryFile.Unpacker.New.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Implementation.ObjectMarshalers.FieldMarshalers
{
    public class OrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType>
        : OrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType, OrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType>>
        where TDeclaringType : class
    {
        public OrderedUnaryFieldMarshaler(string name) : base(name)
        {
        }

        protected Action<TDeclaringType, TFieldType>? fieldSetter { get; set; }
        protected Func<TDeclaringType, TFieldType>? fieldGetter { get; set; }

        public OrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType> From(Func<TDeclaringType, TFieldType> getter)
        {
            IsSerializationEnabled = true;
            fieldGetter = getter;
            return this;
        }

        public OrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType> Into(Action<TDeclaringType, TFieldType> setter)
        {
            IsDeserializationEnabled = true;
            fieldSetter = setter;
            return this;
        }

        public override void DeserializeInto(TDeclaringType mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            fieldByteLengh = 0;

            if (offsetGetter is null)
                throw new Exception($"{Name}. Field Offset has not been specified. Use .AtOffset() config method.");

            int fieldRelativeOffset = offsetGetter(mappedObject);
            var relativeTo = offsetRelationGetter?.Invoke(mappedObject) ?? OffsetRelation.Segment;

            var fieldCtx = new MarshalingContext(Name, ctx.MarshalerStore, ctx, fieldRelativeOffset, relativeTo,
                //TODO implement that metadata
                new MarshalingMetadata(null, null, null));

            if (fieldSetter is null)
                throw new Exception($"{Name}. Field Value Setter has not been specified. Use .Into() config method.");
            if (marshalingValueSetter is null)
                throw new Exception($"{Name}. Field Marshaling Value Setter has not been specified. Use .MarshalInto() config method.");

            var activator = ctx.MarshalerStore.GetActivatorFor<TFieldType>(data, fieldCtx);

            //TODO this is a mess, should Activated type = Marshaled type? Activator with custom logic coudl return derrived class
            //TODO but activator should also be its deserializer. But what if there is Deserializer for Derrived class but Activator returned on base?
            //TODO same conundrum for Unary and Collection :(
            //var deserializer = ctx.MarshalerStore.GetDeserializatorFor<TMarshalingType>();
            var deserializer = activator as IDeserializingMarshaler<TMarshalingType, TMarshalingType>;

            if (deserializer is null)
                throw new Exception($"{Name}. Failed to locate deserializer for '{typeof(TFieldType).Name}'");

            var fieldValue = activator is null ? default : activator.Activate(data, ctx, mappedObject);
            var marshaledValue = fieldValue is null || marshalingValueGetter is null ? default : marshalingValueGetter(mappedObject, fieldValue);

            marshaledValue = deserializer.DeserializeInto(marshaledValue, data, fieldCtx, out fieldByteLengh);

            fieldValue = marshalingValueSetter(mappedObject, fieldValue, marshaledValue);
            fieldSetter(mappedObject, fieldValue);
        }

        public override void SerializeFrom(TDeclaringType mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            fieldByteLengh = 0;

            var serializer = ctx.MarshalerStore.GetSerializatorFor<TMarshalingType>();

            if (serializer is null)
                throw new Exception($"{Name}. Failed to locate serializer for '{typeof(TFieldType).Name}'");

            if (offsetGetter is null)
                throw new Exception($"{Name}. Field Offset has not been specified. Use .AtOffset() config method.");

            int fieldRelativeOffset = offsetGetter(mappedObject);
            var relativeTo = offsetRelationGetter?.Invoke(mappedObject) ?? OffsetRelation.Segment;

            var fieldCtx = new MarshalingContext(Name, ctx.MarshalerStore, ctx, fieldRelativeOffset, relativeTo,
                //TODO implement that metadata
                new MarshalingMetadata(null, null, null));

            if (fieldGetter is null)
                throw new Exception($"{Name}. Field Value Getter has not been specified. Use .From() config method.");
            if (marshalingValueGetter is null)
                throw new Exception($"{Name}. Field Marshaling Value Getter has not been specified. Use .MarshalFrom() config method.");

            var fieldValue = fieldGetter(mappedObject);
            var marshaledValue = marshalingValueGetter(mappedObject, fieldValue);

            serializer.SerializeFrom(marshaledValue, data, fieldCtx, out fieldByteLengh);
        }
    }
}
