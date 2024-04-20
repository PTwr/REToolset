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
    public class OrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType>
        : OrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, IOrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType>>
        , IOrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType>
        where TDeclaringType : class
    {
        public OrderedUnaryFieldMarshaler(string name) : base(name)
        {
        }

        protected Action<TDeclaringType, TFieldType>? fieldSetter { get; set; }
        protected Func<TDeclaringType, TFieldType>? fieldGetter { get; set; }

        public IOrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> From(Func<TDeclaringType, TFieldType> getter)
        {
            IsSerializationEnabled = true;
            fieldGetter = getter;
            return this;
        }

        public IOrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> Into(Action<TDeclaringType, TFieldType> setter)
        {
            IsDeserializationEnabled = true;
            fieldSetter = setter;
            return this;
        }

        public override void DeserializeInto(TDeclaringType mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            fieldByteLength = 0;

            if (offsetGetter is null)
                throw new Exception($"{Name}. Field Offset has not been specified. Use .AtOffset() config method.");

            int fieldRelativeOffset = offsetGetter(mappedObject);
            var relativeTo = offsetRelationGetter?.Invoke(mappedObject) ?? OffsetRelation.Segment;

            MarshalingMetadata metadata = PrepareMetadata(mappedObject);
            var fieldCtx = new MarshalingContext(Name, ctx.MarshalerStore, ctx, fieldRelativeOffset, relativeTo, metadata);
            fieldCtx.WithFieldByteLength(lengthGetter?.Invoke(mappedObject));

            if (fieldSetter is null)
                throw new Exception($"{Name}. Field Value Setter has not been specified. Use .Into() config method.");
            if (marshalingValueSetter is null)
                throw new Exception($"{Name}. Field Marshaling Value Setter has not been specified. Use .MarshalInto() config method.");

            if (Name == "ContentbyPattern")
            {

            }

            var activator = ctx.MarshalerStore.GetActivatorFor<TFieldType>(data, fieldCtx);

            //TODO this is a mess, should Activated type = Marshaled type? Activator with custom logic coudl return derived class
            //TODO but activator should also be its deserializer. But what if there is Deserializer for Derived class but Activator returned on base?
            //TODO same conundrum for Unary and Collection :(
            //var deserializer = ctx.MarshalerStore.GetDeserializatorFor<TMarshalingType>();
            var deserializer = activator is IDeserializingMarshaler<TMarshaledType, TMarshaledType>
                ? activator as IDeserializingMarshaler<TMarshaledType, TMarshaledType>
                : ctx.MarshalerStore.GetDeserializatorFor<TMarshaledType>();

            if (deserializer is null)
                throw new Exception($"{Name}. Failed to locate deserializer for '{typeof(TFieldType).Name}'");

            var fieldValue = activator is null ? default : activator.Activate(data, ctx, mappedObject);
            var marshaledValue = fieldValue is null || marshalingValueGetter is null ? default : marshalingValueGetter(mappedObject, fieldValue);

            marshaledValue = deserializer.DeserializeInto(marshaledValue, data, fieldCtx, out fieldByteLength);

            fieldValue = marshalingValueSetter(mappedObject, fieldValue, marshaledValue);

            Validate(mappedObject, fieldValue);

            fieldSetter(mappedObject, fieldValue);
        }

        //TODO rewrite! fugly!
        protected void Validate(TDeclaringType declaringObject, TFieldType value)
        {
            if (expectedValueGetter is not null)
            {
                var expectedVal = expectedValueGetter(declaringObject);
                var result = EqualityComparer<TFieldType>.Default.Equals(value, expectedVal);
                if (!result) throw new Exception($"{Name}. Unexpected Value! Expected: '{expectedVal}', actual: '{value}'");
            }
            //TODO implement custom validators
            //if (ValidateFunc is not null)
            //{
            //    var result = ValidateFunc.Invoke(declaringObject, value);
            //    if (!result) throw new Exception($"{Name}. Validation failed! Deserialized value: '{value}'");
            //}
        }

        public override void SerializeFrom(TDeclaringType mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            fieldByteLength = 0;

            var serializer = ctx.MarshalerStore.GetSerializatorFor<TMarshaledType>();

            if (serializer is null)
                throw new Exception($"{Name}. Failed to locate serializer for '{typeof(TFieldType).Name}'");

            if (offsetGetter is null)
                throw new Exception($"{Name}. Field Offset has not been specified. Use .AtOffset() config method.");

            int fieldRelativeOffset = offsetGetter(mappedObject);
            var relativeTo = offsetRelationGetter?.Invoke(mappedObject) ?? OffsetRelation.Segment;

            MarshalingMetadata metadata = PrepareMetadata(mappedObject);
            var fieldCtx = new MarshalingContext(Name, ctx.MarshalerStore, ctx, fieldRelativeOffset, relativeTo, metadata);
            fieldCtx.WithFieldByteLength(lengthGetter?.Invoke(mappedObject));

            if (fieldGetter is null)
                throw new Exception($"{Name}. Field Value Getter has not been specified. Use .From() config method.");
            if (marshalingValueGetter is null)
                throw new Exception($"{Name}. Field Marshaling Value Getter has not been specified. Use .MarshalFrom() config method.");

            var fieldValue = fieldGetter(mappedObject);
            var marshaledValue = marshalingValueGetter(mappedObject, fieldValue);

            serializer.SerializeFrom(marshaledValue, data, fieldCtx, out fieldByteLength);

            afterSerializingEvent?.Invoke(mappedObject, fieldByteLength);
        }

        private MarshalingMetadata PrepareMetadata(TDeclaringType mappedObject)
        {
            return new MarshalingMetadata(encodingGetter?.Invoke(mappedObject), littleEndianGetter?.Invoke(mappedObject), nullTermiantionGetter?.Invoke(mappedObject), null);
        }

        Func<TDeclaringType, TFieldType>? expectedValueGetter;
        public IOrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> WithExpectedValueOf(Func<TDeclaringType, TFieldType> expectedValue)
        {
            expectedValueGetter = expectedValue;
            return this;
        }

        public IOrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> WithExpectedValueOf(TFieldType expectedValue)
            => WithExpectedValueOf(i => expectedValue);
    }
}
