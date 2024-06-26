﻿using BinaryDataHelper;
using BinaryFile.Marshaling.Common;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.FieldMarshaling
{
    public class OrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType>
            : OrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType>>
            , IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> where TDeclaringType : class
    {
        public OrderedCollectionFieldMarshaler(string name) : base(name)
        {
        }

        protected Action<TDeclaringType, IEnumerable<TFieldType>>? fieldSetter { get; set; }
        protected Func<TDeclaringType, IEnumerable<TFieldType>>? fieldGetter { get; set; }

        public IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> From(Func<TDeclaringType, IEnumerable<TFieldType>> getter)
        {
            IsSerializationEnabled = true;
            fieldGetter = getter;
            return this;
        }

        public IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> Into(Action<TDeclaringType, IEnumerable<TFieldType>> setter)
        {
            IsDeserializationEnabled = true;
            fieldSetter = setter;
            return this;
        }

        public override void DeserializeInto(TDeclaringType mappedObject, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            fieldByteLength = 0;

            if (offsetGetter is null)
                throw new Exception($"{Name}. Field Offset has not been specified. Use .AtOffset() config method.");

            int fieldRelativeOffset = offsetGetter(mappedObject);
            var relativeTo = offsetRelationGetter?.Invoke(mappedObject) ?? OffsetRelation.Segment;

            var metadata = PrepareMetadata(mappedObject);
            var fieldCtx = new MarshalingContext(Name, ctx.MarshalerStore, ctx, fieldRelativeOffset, relativeTo, metadata);

            fieldCtx.WithFieldByteLength(lengthGetter?.Invoke(mappedObject));
            fieldCtx.WithItemByteLength(itemLengthGetter?.Invoke(mappedObject));

            if (fieldSetter is null)
                throw new Exception($"{Name}. Field Value Setter has not been specified. Use .Into() config method.");
            if (marshalingValueSetter is null)
                throw new Exception($"{Name}. Field Marshaling Value Setter has not been specified. Use .MarshalInto() config method.");

            var availableBytes = fieldCtx.ItemSlice(data).Length;
            int maxAbsoluteItemOffset = fieldCtx.ItemAbsoluteOffset + availableBytes;

            List<KeyValuePair<int, TFieldType>> Items = new List<KeyValuePair<int, TFieldType>>();
            var count = countGetter?.Invoke(mappedObject);
            int itemOffset = 0;

            for (int itemNumber = 0; ; itemNumber++)
            {
                //TODO BreakWhen, bytelength
                if (count is not null && itemNumber >= count) break;

                //align item start
                var byteAlignment = itemByteAlignmentGetter?.Invoke(mappedObject);
                if (byteAlignment is not null)
                {
                    itemOffset = itemOffset.Align(byteAlignment.Value);
                }

                fieldCtx.WithItemOffset(itemOffset);

                if (BreakWhenEvent is not null && BreakWhenEvent(mappedObject, Items.Select(i => i.Value), data, fieldCtx))
                    break;

                if (fieldCtx.ItemAbsoluteOffset >= maxAbsoluteItemOffset)
                {
                    if (count is not null)
                        throw new Exception($"{Name}. Item offset of {itemOffset} (abs {fieldCtx.ItemAbsoluteOffset}) for item #{itemNumber} reaches out of bounds of field slice of {availableBytes} bytes, with expected count of {count}.");

                    break;
                }

                var marshaledValueMarshaler = ctx.MarshalerStore.FindMarshaler<TMarshaledType>();

                if (marshaledValueMarshaler is null)
                    throw new Exception($"{Name}. Failed to locate marshaler for '{typeof(TFieldType).Name}'");

                var fieldTypeMarshaler = ctx.MarshalerStore.FindMarshaler<TFieldType>() as MarshalerWrapper<TFieldType>;

                TFieldType? fieldValue = fieldTypeMarshaler is null ? default : fieldTypeMarshaler.Activate(mappedObject, data, fieldCtx);
                var marshaledValue = fieldValue is null || marshalingValueGetter is null ? default : marshalingValueGetter(mappedObject, fieldValue);

                marshaledValue = marshaledValueMarshaler.Deserialize(marshaledValue, mappedObject, data, fieldCtx, out var itemLength);

                fieldValue = marshalingValueSetter(mappedObject, fieldValue, marshaledValue);

                Validate(mappedObject, fieldValue);

                Items.Add(new KeyValuePair<int, TFieldType>(itemOffset, fieldValue));

                itemLength = itemLengthGetter?.Invoke(mappedObject) ?? itemLength;

                itemOffset += itemLength;

                //align item end by nullpadding
                var bytePadding = itemNullPadToAlignmentGetter?.Invoke(mappedObject);
                if (bytePadding is not null)
                {
                    itemOffset = itemOffset.Align(bytePadding.Value);
                }

                //TODO implement
                //fieldSetterUnary(mappedObject, fieldValue);
            }
            fieldSetter?.Invoke(mappedObject, Items.Select(i => i.Value));
        }

        public override void SerializeFrom(TDeclaringType mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            fieldByteLength = 0;

            var marshaler = ctx.MarshalerStore.FindMarshaler<TMarshaledType>();

            if (marshaler is null)
                throw new Exception($"{Name}. Failed to locate marshaler for '{typeof(TFieldType).Name}'");

            if (offsetGetter is null)
                throw new Exception($"{Name}. Field Offset has not been specified. Use .AtOffset() config method.");

            int fieldRelativeOffset = offsetGetter(mappedObject);
            var relativeTo = offsetRelationGetter?.Invoke(mappedObject) ?? OffsetRelation.Segment;

            var metadata = PrepareMetadata(mappedObject);
            var fieldCtx = new MarshalingContext(Name, ctx.MarshalerStore, ctx, fieldRelativeOffset, relativeTo, metadata);

            if (fieldGetter is null)
                throw new Exception($"{Name}. Field Value Getter has not been specified. Use .From() config method.");
            if (marshalingValueGetter is null)
                throw new Exception($"{Name}. Field Marshaling Value Getter has not been specified. Use .MarshalFrom() config method.");

            var fieldValues = fieldGetter(mappedObject);

            int itemOffset = 0;
            int itemNumber = 0;
            foreach (var fieldValue in fieldValues)
            {
                //align item start
                var byteAlignment = itemByteAlignmentGetter?.Invoke(mappedObject);
                if (byteAlignment is not null)
                {
                    itemOffset = itemOffset.Align(byteAlignment.Value);
                }

                fieldCtx.WithItemOffset(itemOffset);

                var marshaledValue = marshalingValueGetter(mappedObject, fieldValue);

                marshaler.Serialize(marshaledValue, data, fieldCtx, out var itemLength);

                itemLength = itemLengthGetter?.Invoke(mappedObject) ?? itemLength;

                afterSerializingItemEvent?.Invoke(mappedObject, fieldValue, itemNumber, itemLength, itemOffset);

                itemOffset += itemLength;
                itemNumber++;

                //align item end by nullpadding
                var bytePadding = itemNullPadToAlignmentGetter?.Invoke(mappedObject);
                if (bytePadding is not null)
                {
                    itemOffset = itemOffset.Align(bytePadding.Value, out var paddedby);
                    data.ResizeToAtLeast(fieldCtx.FieldAbsoluteOffset + itemOffset);
                }
            }

            fieldByteLength = itemOffset;

            afterSerializingEvent?.Invoke(mappedObject, fieldByteLength);
        }

        private IMarshalingMetadata PrepareMetadata(TDeclaringType mappedObject)
        {
            return new MarshalingMetadata(encodingGetter?.Invoke(mappedObject), littleEndianGetter?.Invoke(mappedObject), nullTermiantionGetter?.Invoke(mappedObject), countGetter?.Invoke(mappedObject));
        }

        IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType>.BreakWhenDelegate? BreakWhenEvent;
        public IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> BreakWhen(
            IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType>.BreakWhenDelegate handler)
        {
            BreakWhenEvent = handler;
            return this;
        }

        Func<TDeclaringType, int>? countGetter;
        public IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> WithCountOf(Func<TDeclaringType, int> func)
        {
            countGetter = func;
            return this;
        }
        public IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> WithCountOf(int count) => WithCountOf(i => count);

        protected Func<TDeclaringType, int>? itemLengthGetter;
        public IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> WithItemByteLengthOf(Func<TDeclaringType, int> func)
        {
            itemLengthGetter = func;
            return this;
        }
        public IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> WithItemByteLengthOf(int itemLength) => WithItemByteLengthOf(i => itemLength);

        protected Func<TDeclaringType, int>? itemByteAlignmentGetter;
        public IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> WithItemByteAlignment(Func<TDeclaringType, int> func)
        {
            itemByteAlignmentGetter = func;
            return this;
        }
        public IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> WithItemByteAlignment(int padding) => WithItemByteAlignment(i => padding);

        protected Func<TDeclaringType, int>? itemNullPadToAlignmentGetter;
        public IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> WithItemNullPadToAlignment(Func<TDeclaringType, int> func)
        {
            itemNullPadToAlignmentGetter = func;
            return this;
        }
        public IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> WithItemNullPadToAlignment(int padding) => WithItemNullPadToAlignment(i => padding);

        IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType>>
            .AfterSerializingItemEvent afterSerializingItemEvent;
        public IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType> AfterSerializingItem(IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType>>.AfterSerializingItemEvent hook)
        {
            afterSerializingItemEvent = hook;
            return this;
        }
    }
}
