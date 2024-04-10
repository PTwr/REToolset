using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Marshalers.Fluent
{
    public class FluentCollectionFieldMarshaler<TDeclaringType, TItem> : FluentCollectionFieldMarshaler<TDeclaringType, TItem, TItem>
    {
        public FluentCollectionFieldMarshaler(string? name) : base(name)
        {
        }

        protected override TItem GetMarshalingValue(TDeclaringType declaringObject, TItem item)
        {
            return item;
        }

        //TODO rewrite method header, this is ExtractMethod garbage :D
        protected override void DeserializeItem(TDeclaringType declaringObject, Span<byte> bytes, out int consumedLength, FluentMarshalingContext<TDeclaringType, TItem> itemContext, IDeserializer<TItem>? deserializer, out TItem? marshaledValue, out TItem? item)
        {
            marshaledValue = deserializer.Deserialize(bytes, itemContext, out consumedLength);
            item = marshaledValue;
        }
    }
    public class FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> :
        BaseFluentFieldMarshaler<TDeclaringType, TItem, FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType>>,
        IFieldMarshaler<TDeclaringType>,
        IFluentCollectionFieldMarshaler<TDeclaringType, TItem, FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType>>
    {
        public FluentCollectionFieldMarshaler(string? name) : base(name)
        {
        }

        MarshalingValueSetter? marshalingValueSetter;
        public delegate TItem MarshalingValueSetter(TDeclaringType declaringObject, TItem item, TMarshalingType marshalingType);
        public virtual TItem SetMarshalingValue(TDeclaringType declaringObject, TItem item, TMarshalingType marshaledValue)
        {
            if (marshalingValueSetter is not null)
                return marshalingValueSetter(declaringObject, item, marshaledValue);
            return item;
        }
        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> WithMarshalingValueSetter(MarshalingValueSetter setter)
        {
            marshalingValueSetter = setter;
            return this;
        }

        MarshalingValueGetter? marshalingValueGetter = null;
        public delegate TMarshalingType MarshalingValueGetter(TDeclaringType declaringObject, TItem item);
        protected virtual TMarshalingType GetMarshalingValue(TDeclaringType declaringObject, TItem item)
        {
            if (marshalingValueGetter is null) throw new NullReferenceException($"{Name}. Getter for marshaled value has not been set!");

            return marshalingValueGetter(declaringObject, item);
        }
        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> WithMarshalingValueGetter(MarshalingValueGetter getter)
        {
            marshalingValueGetter = getter;
            return this;
        }

        //TODO delegates? Or events of delegates?
        protected Action<TDeclaringType, IEnumerable<TItem>>? Setter { get; set; }
        public delegate void SetterUnaryDelegate(TDeclaringType declaringObject, TItem item, TMarshalingType marshaledValue, int index, int relativeOffset);
        protected SetterUnaryDelegate? SetterUnary { get; set; }
        protected Func<TDeclaringType, IEnumerable<TItem>>? Getter { get; set; }

        protected FuncField<TDeclaringType, IEnumerable<TItem>>? ExpectedValue { get; set; }
        protected Func<TDeclaringType, IEnumerable<TItem>, bool>? ValidateFunc { get; set; }

        protected Func<TDeclaringType, IEnumerable<TItem>, bool>? BreakWhenFunc { get; set; }

        public void Deserialize(TDeclaringType declaringObject, Span<byte> bytes, IMarshalingContext context, out int consumedLength)
        {
            if (Setter is null && SetterUnary is null)
                throw new Exception($"{this}. Setter has not been provided!");

            consumedLength = 0;
            if (declaringObject == null)
                throw new ArgumentException($"{Name}. Declaring object is required for Fluent Deserialization!");

            if (WhenSimple is not null && WhenSimple.Get(declaringObject) is false) return;
            if (DeserializationWhenSimple is not null && DeserializationWhenSimple.Get(declaringObject) is false) return;

            List<KeyValuePair<int, TItem>> Items = new List<KeyValuePair<int, TItem>>();

            int collectionRelativeOffset = Offset?.Get(declaringObject)
                ?? throw new Exception($"{Name}.Neither Offset nor OffsetFunc has been set!");

            var l = context.Length;
            var fieldContext = new FluentMarshalingContext<TDeclaringType, TItem>(Name, context, OffsetRelation, collectionRelativeOffset, Metadata, declaringObject, 0);
            var ll = fieldContext.Length;

            var count = Metadata?.Count?.Get(declaringObject);

            int itemOffsetCorrection = 0;
            for (int itemNumber = 0; ; itemNumber++)
            {
                if (count is not null && itemNumber >= count) break;

                //TODO handle alignment on read?
                var byteAlignment = ItemByteAlignment?.Get(declaringObject);
                if (byteAlignment is not null)
                {
                    //itemOffset = itemOffset.Align(byteAlignment.Value, out var paddedby);
                    //itemOffsetCorrection += paddedby;
                    itemOffsetCorrection = itemOffsetCorrection.Align(byteAlignment.Value);
                }

                //TODO FieldLength gets mixed up as Item Length when Slicing! Gotta fork Metadata here per-item lengths
                //TODO maybe collection-specific Ctx to handle item offset and item length internally?
                var itemContext = new FluentMarshalingContext<TDeclaringType, TItem>(Name, context, OffsetRelation, collectionRelativeOffset, Metadata, declaringObject, itemOffsetCorrection);

                if (BreakWhenFunc is not null)
                    if (BreakWhenFunc(declaringObject, Items.Select(i => i.Value)))
                        break;
                if (BreakWhenEvent is not null)
                    if (BreakWhenEvent(declaringObject, Items.Select(i => i.Value), bytes, itemContext))
                        break;

                var availableBytes = itemContext.Slice(bytes).Length;
                int maxAbsoluteItemOffset = itemContext.AbsoluteOffset + availableBytes;

                if (itemContext.AbsoluteOffset >= maxAbsoluteItemOffset)
                {
                    if (count is not null)
                        throw new Exception($"{Name}. Item offset of {itemOffsetCorrection} (abs {itemContext.AbsoluteOffset}) for item #{itemNumber} exceedes count limits {count} of field slice of {availableBytes} bytes.");

                    break;
                }

                IDeserializer<TMarshalingType>? deserializer = null;
                if (this.customMappingSelector is not null) deserializer = this.customMappingSelector(bytes, itemContext);
                else if (context.DeserializerManager.TryGetMapping<TMarshalingType>(out deserializer) is false) throw new Exception($"{Name}. Deserializer for {typeof(TItem).FullName} not found.");

                TMarshalingType? marshaledValue;
                TItem? item;
                DeserializeItem(declaringObject, bytes, out consumedLength, itemContext, deserializer, out marshaledValue, out item);

                if (Metadata.ItemLength is not null) consumedLength = Metadata.ItemLength.Get(declaringObject, item);

                //TODO add option to disable this error?
                if (consumedLength <= 0)
                    throw new Exception($"{Name}. Non-positive item consumed byte length of {consumedLength}!");

                if (SetterUnary is not null)
                    SetterUnary(declaringObject, item, marshaledValue, Items.Count, itemOffsetCorrection);

                //TODO error on offset dups? but previous check should prevent dups
                Items.Add(new KeyValuePair<int, TItem>(itemOffsetCorrection, item));

                itemOffsetCorrection += consumedLength;

                var bytePadding = ItemNullPadToAlignment?.Get(declaringObject);
                if (bytePadding is not null)
                {
                    itemOffsetCorrection = itemOffsetCorrection.Align(bytePadding.Value, out var paddedby);
                }
            }

            Validate(declaringObject, Items.Select(i => i.Value));

            //TODO alternative Setters accepting (offset, value) pairs (GEV/OFS will need that)
            Setter?.Invoke(declaringObject, Items.Select(i => i.Value));
        }

        protected virtual void DeserializeItem(TDeclaringType declaringObject, Span<byte> bytes, out int consumedLength, FluentMarshalingContext<TDeclaringType, TItem> itemContext, IDeserializer<TMarshalingType>? deserializer, out TMarshalingType? marshaledValue, out TItem? item)
        {
            marshaledValue = deserializer.Deserialize(bytes, itemContext, out consumedLength);
            item = SetMarshalingValue(declaringObject, itemContext.Activate<TItem>(), marshaledValue);
        }

        CustomMappingSelector? customMappingSelector = null;
        public delegate IDeserializer<TMarshalingType> CustomMappingSelector(Span<byte> data, IMarshalingContext ctx);
        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> WithCustomMappingSelector(
            CustomMappingSelector selector
            )
        {
            this.customMappingSelector = selector;

            return this;
        }

        //TODO rewrite! fugly!
        public void Validate(TDeclaringType declaringObject, IEnumerable<TItem> values)
        {
            if (ExpectedValue is not null)
            {
                var expectedVal = ExpectedValue.Get(declaringObject);
                var result = expectedVal is null ? values == null : expectedVal.SequenceEqual(values);
                //TODO figure out better exception for displaying items :D
                if (!result) throw new Exception($"{Name}. Unexpected Value! Expected: '{expectedVal}', actual: '{values}'");
            }
            if (ValidateFunc is not null)
            {
                var result = ValidateFunc.Invoke(declaringObject, values);
                //TODO figure out better exception for displaying items :D
                if (!result) throw new Exception($"{Name}. Validation failed! Deserialized values: '{values}'");
            }
        }

        public void Serialize(TDeclaringType declaringObject, ByteBuffer buffer, IMarshalingContext context, out int consumedLength)
        {
            consumedLength = 0;
            if (Getter == null) throw new Exception($"{Name}. Getter has not been provided!");

            if (WhenSimple is not null && WhenSimple.Get(declaringObject) is false) return;
            if (SerializationWhenSimple is not null && SerializationWhenSimple.Get(declaringObject) is false) return;

            var v = Getter(declaringObject);

            //TODO compare against Count
            //TODO make such checks optional? In many cases it would be just annoying extra step of updating Count before serializing

            //TODO implement conditional (de)serialization :D
            if (v == null) throw new Exception($"{Name}. Field value is null! Check for errors and consider using conditional serialization!");

            int collectionRelativeOffset = Offset?.Get(declaringObject) ?? throw new Exception($"{this}. Neither Offset nor OffsetFunc has been set!");

            var fieldContext = new FluentMarshalingContext<TDeclaringType, TItem>(Name, context, OffsetRelation, collectionRelativeOffset, Metadata, declaringObject, offsetCorrection: 0);

            int itemOffsetCorrection = 0;
            int itemNumber = 0;
            foreach (var item in v)
            {
                //TODO various length checks


                //TODO why does Deserialization pass correction to Ctx but Serialization calculates directly???
                //var itemOffset = collectionRelativeOffset + itemOffsetCorrection;

                //TODO differentiate between alignment of absolute offset and alignment of relative offset?
                //TODO this is fucked up, rething and rewrite.
                //TODO Maybe just move alignment to context? but that would result in it being applied to absolute?
                //TODO either way alignment pad has to be includded in offsetcorrection
                var byteAlignment = ItemByteAlignment?.Get(declaringObject);
                if (byteAlignment is not null)
                {
                    itemOffsetCorrection = itemOffsetCorrection.Align(byteAlignment.Value);
                }

                var itemContext = new FluentMarshalingContext<TDeclaringType, TItem>(Name, context, OffsetRelation, collectionRelativeOffset, Metadata, declaringObject, itemOffsetCorrection);

                var marshaledItem = GetMarshalingValue(declaringObject, item);

                //TODO gotta check on item.GetType() to get derrived map :/
                //TODO start with DeserializerSelector? any future automation can be built on top of it
                if (context.SerializerManager.TryGetMapping<TMarshalingType>(out var serializer) is false) throw new Exception($"{Name}. Type Mapping for {typeof(TItem).FullName} not found!");

                serializer.Serialize(marshaledItem, buffer, itemContext, out consumedLength);

                if (Metadata.ItemLength is not null) consumedLength = Metadata.ItemLength.Get(declaringObject, item);

                PostProcessItemByteLength?.Invoke(declaringObject, item, itemNumber, consumedLength, itemOffsetCorrection);

                itemOffsetCorrection += consumedLength;

                var bytePadding = ItemNullPadToAlignment?.Get(declaringObject);
                if (bytePadding is not null)
                {
                    itemOffsetCorrection = itemOffsetCorrection.Align(bytePadding.Value, out var paddedby);
                    buffer.ResizeToAtLeast(fieldContext.AbsoluteOffset + itemOffsetCorrection);
                }

                itemNumber++;
            }

            consumedLength = itemOffsetCorrection;
            PostProcessByteLength?.Invoke(declaringObject, itemOffsetCorrection);
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> From(Func<TDeclaringType, IEnumerable<TItem>> getter)
        {
            SerializationInitialized = true;
            Getter = getter;
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> Into(Action<TDeclaringType, IEnumerable<TItem>> setter)
        {
            DeserializationInitialized = true;
            Setter = setter;
            return this;
        }
        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> Into(SetterUnaryDelegate setter)
        {
            DeserializationInitialized = true;
            SetterUnary = setter;
            return this;
        }

        BreakWhenDelegate? BreakWhenEvent;
        public delegate bool BreakWhenDelegate(TDeclaringType declaringObject, IEnumerable<TItem> items, Span<byte> data, IMarshalingContext context);
        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> BreakWhen(BreakWhenDelegate breakWhen)
        {
            BreakWhenEvent = breakWhen;
            return this;
        }
        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> BreakWhen(Func<TDeclaringType, IEnumerable<TItem>, bool> breakPredicate)
        {
            BreakWhenFunc = breakPredicate;
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> WithCountOf(int count)
        {
            Metadata.Count = new FuncField<TDeclaringType, int>(count);
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> WithCountOf(Func<TDeclaringType, int> countFunc)
        {
            Metadata.Count = new FuncField<TDeclaringType, int>(countFunc);
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> WithItemLengthOf(int itemLength)
        {
            Metadata.ItemLength = new FuncField<TDeclaringType, TItem, int>(itemLength);
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> WithItemLengthOf(Func<TDeclaringType, TItem, int> itemLengthFunc)
        {
            Metadata.ItemLength = new FuncField<TDeclaringType, TItem, int>(itemLengthFunc);
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> WithExpectedValueOf(IEnumerable<TItem> expectedValue)
        {
            ExpectedValue = new FuncField<TDeclaringType, IEnumerable<TItem>>(expectedValue);
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> WithExpectedValueOf(Func<TDeclaringType, IEnumerable<TItem>> expectedValuefunc)
        {
            ExpectedValue = new FuncField<TDeclaringType, IEnumerable<TItem>>(expectedValuefunc);
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> WithValidator(Func<TDeclaringType, IEnumerable<TItem>, bool> validateFunc)
        {
            ValidateFunc = validateFunc;
            return this;
        }
        
        FuncField<TDeclaringType, int>? ItemByteAlignment { get; set; }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> WithItemByteAlignment(int alignmentInBytes)
        {
            ItemByteAlignment = new FuncField<TDeclaringType, int>(alignmentInBytes);
            return this;
        }
        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> WithItemByteAlignment(Func<TDeclaringType, int> alignmentInBytesFunc)
        {
            ItemByteAlignment = new FuncField<TDeclaringType, int>(alignmentInBytesFunc);
            return this;
        }

        FuncField<TDeclaringType, int>? ItemNullPadToAlignment { get; set; }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> WithItemNullPadToAlignment(int alignmentInBytes)
        {
            ItemNullPadToAlignment = new FuncField<TDeclaringType, int>(alignmentInBytes);
            return this;
        }
        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> WithItemNullPadToAlignment(Func<TDeclaringType, int> alignmentInBytesFunc)
        {
            ItemNullPadToAlignment = new FuncField<TDeclaringType, int>(alignmentInBytesFunc);
            return this;
        }

        protected IFluentFieldDescriptorEvents<TDeclaringType, TItem, FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType>>
            .PostProcessCollectionDelegate?
            PostProcessByteLength { get; set; }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> AfterSerializing(
            IFluentFieldDescriptorEvents<TDeclaringType, TItem, FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType>>
            .PostProcessCollectionDelegate postProcessByteLength)
        {
            PostProcessByteLength = postProcessByteLength;
            return this;
        }

        protected IFluentFieldDescriptorEvents<TDeclaringType, TItem, FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType>>
            .PostProcessCollectionItemDelegate?
            PostProcessItemByteLength { get; set; }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType> AfterSerializing(
            IFluentFieldDescriptorEvents<TDeclaringType, TItem, FluentCollectionFieldMarshaler<TDeclaringType, TItem, TMarshalingType>>
            .PostProcessCollectionItemDelegate postProcessItemByteLength)
        {
            PostProcessItemByteLength = postProcessItemByteLength;
            return this;
        }
    }
}
