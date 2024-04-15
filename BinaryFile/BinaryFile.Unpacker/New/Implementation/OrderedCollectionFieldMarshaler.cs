using BinaryDataHelper;
using BinaryFile.Unpacker.New.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Implementation
{
    public class OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType>
        : OrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType, OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType>>
        where TDeclaringType : class
    {
        public OrderedCollectionFieldMarshaler(string name) : base(name)
        {
        }

        protected Action<TDeclaringType, IEnumerable<TFieldType>>? fieldSetter { get; set; }
        protected Func<TDeclaringType, IEnumerable<TFieldType>>? fieldGetter { get; set; }

        public OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType> From(Func<TDeclaringType, IEnumerable<TFieldType>> getter)
        {
            IsSerializationEnabled = true;
            fieldGetter = getter;
            return this;
        }

        public OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType> Into(Action<TDeclaringType, IEnumerable<TFieldType>> setter)
        {
            IsDeserializationEnabled = true;
            this.fieldSetter = setter;
            return this;
        }

        public override void DeserializeInto(TDeclaringType mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            throw new NotImplementedException();
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

            var fieldValues = fieldGetter(mappedObject);

            int itemOffset = 0;
            int n = 0;
            foreach(var fieldValue in fieldValues)
            {
                //align item start
                var byteAlignment = itemByteAlignmentGetter?.Invoke(mappedObject);
                if (byteAlignment is not null)
                {
                    itemOffset = itemOffset.Align(byteAlignment.Value);
                }

                fieldCtx.CorrectForCollectionItem(itemOffset, itemLengthGetter?.Invoke(mappedObject));

                var marshaledValue = marshalingValueGetter(mappedObject, fieldValue);

                serializer.SerializeFrom(marshaledValue, data, fieldCtx, out var itemLength);

                itemOffset += itemLength;
                n++;

                //align item end by nullpadding
                var bytePadding = itemNullPadToAlignmentGetter?.Invoke(mappedObject);
                if (bytePadding is not null)
                {
                    itemOffset = itemOffset.Align(bytePadding.Value, out var paddedby);
                    data.ResizeToAtLeast(fieldCtx.FieldAbsoluteOffset + itemOffset);
                }
            }

            fieldByteLengh = itemOffset;
        }

        BreakWhenDelegate? BreakWhenEvent;
        public delegate bool BreakWhenDelegate(TDeclaringType declaringObject, IEnumerable<TFieldType> items, Span<byte> data, IMarshalingContext context);
        public OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType> BreakWhen(BreakWhenDelegate handler)
        {
            BreakWhenEvent = handler;
            return this;
        }

        public OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType> Config()
        {
            return this;
        }

        Func<TDeclaringType, int>? countGetter;
        public OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType> WithCountOf(Func<TDeclaringType, int> func)
        {
            countGetter = func;
            return this;
        }
        public OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType> WithCountOf(int count) => WithCountOf(i => count);

        protected Func<TDeclaringType, int>? itemLengthGetter;
        public OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType> WithItemByteLengthOf(Func<TDeclaringType, int> func)
        {
            itemLengthGetter = func;
            return this;
        }
        public OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType> WithItemByteLengthOf(int itemLength) => WithItemByteLengthOf(i => itemLength);

        protected Func<TDeclaringType, int>? itemByteAlignmentGetter;
        public OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType> WithItemByteAlignment(Func<TDeclaringType, int> func)
        {
            itemByteAlignmentGetter = func;
            return this;
        }
        public OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType> WithItemByteAlignment(int padding) => WithItemByteAlignment(i => padding);

        protected Func<TDeclaringType, int>? itemNullPadToAlignmentGetter;
        public OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType> WithItemNullPadToAlignment(Func<TDeclaringType, int> func)
        {
            itemNullPadToAlignmentGetter = func;
            return this;
        }
        public OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshalingType> WithItemNullPadToAlignment(int padding) => WithItemNullPadToAlignment(i => padding);
    }
}
