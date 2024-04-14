using BinaryDataHelper;
using BinaryFile.Unpacker.Marshalers.Fluent;
using BinaryFile.Unpacker.New.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Implementation
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
            this.fieldSetter = setter;
            return this;
        }

        public override void DeserializeInto(TDeclaringType mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            fieldByteLengh = 0;

            var deserializer = ctx.MarshalerStore.GetDeserializatorFor<TMarshalingType>();

            if (deserializer is null)
                throw new Exception($"{Name}. Failed to locate deserializer for '{typeof(TFieldType).Name}'");

            if (offsetGetter is null)
                throw new Exception($"{Name}. Field Offset has not been specified. Use .AtOffset() config method.");

            int fieldRelativeOffset = offsetGetter(mappedObject);
            var relativeTo = offsetRelationGetter?.Invoke(mappedObject) ?? Metadata.OffsetRelation.Segment;

            var fieldCtx = new MarshalingContext(Name, ctx.MarshalerStore, ctx, fieldRelativeOffset, relativeTo);

            if (fieldGetter is null)
                throw new Exception($"{Name}. Field Value Getter has not been specified. Use .From() config method.");
            if (marshalingValueGetter is null)
                throw new Exception($"{Name}. Field Marshaling Value Getter has not been specified. Use .MarshalInto() config method.");

            var fieldValue = fieldGetter(mappedObject);
            var marshaledValue = marshalingValueGetter(mappedObject, fieldValue);

            //TODO deserializeInto pattern will not work with value type, and ref value will not work due to co(ntra)ovariance
            //deserializer.DeserializeInto()
        }

        public override void SerializeFrom(TDeclaringType mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            throw new NotImplementedException();
        }
    }
}
