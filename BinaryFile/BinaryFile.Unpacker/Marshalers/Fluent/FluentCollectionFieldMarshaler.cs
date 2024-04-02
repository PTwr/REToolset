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
    public class FluentCollectionFieldMarshaler<TDeclaringType, TItem> :
        BaseFluentFieldMarshaler<TDeclaringType, TItem, FluentCollectionFieldMarshaler<TDeclaringType, TItem>>,
        IFieldMarshaler<TDeclaringType>,
        IFluentCollectionFieldMarshaler<TDeclaringType, TItem, FluentCollectionFieldMarshaler<TDeclaringType, TItem>>
    {
        public FluentCollectionFieldMarshaler(string name) : base(name)
        {
        }

        protected Action<TDeclaringType, IEnumerable<TItem>>? Setter { get; set; }
        protected Func<TDeclaringType, IEnumerable<TItem>>? Getter { get; set; }

        protected FuncField<TDeclaringType, IEnumerable<TItem>>? ExpectedValue { get; set; }
        protected Func<TDeclaringType, IEnumerable<TItem>, bool>? ValidateFunc { get; set; }

        protected Func<TDeclaringType, IEnumerable<TItem>, bool>? BreakWhenFunc { get; set; }

        public void Deserialize(TDeclaringType declaringObject, Span<byte> bytes, IMarshalingContext context, out int consumedLength)
        {
            if (Setter == null) throw new Exception($"{this}. Setter has not been provided!");

            consumedLength = 0;
            if (declaringObject == null) throw new ArgumentException($"{Name}. Declaring object is required for Fluent Deserialization!");

            var availableBytes = context.Slice(bytes).Length;
            int maxAbsoluteItemOffset = context.AbsoluteOffset + availableBytes;

            List<KeyValuePair<int, TItem>> Items = new List<KeyValuePair<int, TItem>>();

            int itemOffsetCorrection = 0;
            for (int itemNumber = 0; ; itemNumber++)
            {
                if (Metadata.Count is not null && itemNumber >= Metadata.Count.Get(declaringObject)) break;

                int collectionRelativeOffset = Offset?.Get(declaringObject) ?? throw new Exception($"{Name}.Neither Offset nor OffsetFunc has been set!");
                var itemRelativeOffset = collectionRelativeOffset + itemOffsetCorrection;
                var itemContext = new FluentMarshalingContext<TDeclaringType, TItem>(Name, context, OffsetRelation, itemRelativeOffset, Metadata, declaringObject);

                if (itemContext.AbsoluteOffset > bytes.Length)
                    throw new Exception($"{Name}. Absolute offset of {itemContext.AbsoluteOffset} is larger than dataset of {bytes.Length} bytes.");

                if (itemContext.AbsoluteOffset > maxAbsoluteItemOffset)
                    throw new Exception($"{Name}. Item offset of {itemOffsetCorrection} (abs {itemContext.AbsoluteOffset}) for item #{itemNumber} exceedes limits of field slice of {availableBytes} bytes.");

                if (context.DeserializerManager.TryGetMapping<TItem>(out var deserializer) is false) throw new Exception($"{Name}. Deserializer for {typeof(TItem).FullName} not found.");

                var item = deserializer.Deserialize(bytes, itemContext, out consumedLength);

                if (Metadata.ItemLength is not null) consumedLength = Metadata.ItemLength.Get(declaringObject);

                //TODO add option to disable this error?
                if (consumedLength <= 0) throw new Exception($"{Name}. Non-positive item consumed byte length of {consumedLength}!");

                //TODO error on offset dups? but previous check should prevent dups
                Items.Add(new KeyValuePair<int, TItem>(itemOffsetCorrection, item));

                itemOffsetCorrection += consumedLength;
            }

            Validate(declaringObject, Items.Select(i => i.Value));

            //TODO alternative Setters accepting (offset, value) pairs (GEV/OFS will need that)
            Setter(declaringObject, Items.Select(i => i.Value));
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

        public void Serialize(TDeclaringType declaringObject, ByteBuffer buffer, IMarshalingContext serializationContext, out int consumedLength)
        {
            throw new NotImplementedException();
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem> From(Func<TDeclaringType, IEnumerable<TItem>> getter)
        {
            Getter = getter;
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem> Into(Action<TDeclaringType, IEnumerable<TItem>> setter)
        {
            Setter = setter;
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem> BreakWhen(Func<TDeclaringType, IEnumerable<TItem>, bool> breakPredicate)
        {
            BreakWhenFunc = breakPredicate;
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem> WithCountOf(int count)
        {
            Metadata.Count = new FuncField<TDeclaringType, int>(count);
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem> WithCountOf(Func<TDeclaringType, int> countFunc)
        {
            Metadata.Count = new FuncField<TDeclaringType, int>(countFunc);
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem> WithItemLengthOf(int itemLength)
        {
            Metadata.ItemLength = new FuncField<TDeclaringType, int>(itemLength);
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem> WithItemLengthOf(Func<TDeclaringType, int> itemLengthFunc)
        {
            Metadata.ItemLength = new FuncField<TDeclaringType, int>(itemLengthFunc);
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem> WithExpectedValueOf(IEnumerable<TItem> expectedValue)
        {
            ExpectedValue = new FuncField<TDeclaringType, IEnumerable<TItem>>(expectedValue);
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem> WithExpectedValueOf(Func<TDeclaringType, IEnumerable<TItem>> expectedValuefunc)
        {
            ExpectedValue = new FuncField<TDeclaringType, IEnumerable<TItem>>(expectedValuefunc);
            return this;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem> WithValidator(Func<TDeclaringType, IEnumerable<TItem>, bool> validateFunc)
        {
            ValidateFunc = validateFunc;
            return this;
        }
    }
}
