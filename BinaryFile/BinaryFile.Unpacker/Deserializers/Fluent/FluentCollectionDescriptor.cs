using BinaryFile.Unpacker.Metadata;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Deserializers.Fluent
{
    public interface IFluentCollectionDescriptor<TDeclaringType, TItem, TImplementation> :
        IBaseFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentCollectionFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentValidatedCollectionFieldDescriptor<TDeclaringType, TItem, TImplementation>
        where TImplementation : _BaseFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>
    {
    }

    //TODO this got a bit messy, move FluentConfig to base class?
    //TODO add item offset byte alignment
    public class FluentCollectionDescriptor<TDeclaringType, TItem> : 
        _BaseFluentFieldDescriptor<TDeclaringType, TItem, FluentCollectionDescriptor<TDeclaringType, TItem>>,
        IFluentCollectionDescriptor<TDeclaringType, TItem, FluentCollectionDescriptor<TDeclaringType, TItem>>
    {
        public FluentCollectionDescriptor(string? name) : base(name)
        {
        }

        public FuncField<TDeclaringType, IEnumerable<TItem>>? ExpectedValue { get; protected set; }
        public Func<TDeclaringType, IEnumerable<TItem>, bool>? ValidateFunc { get; protected set; }

        public FuncField<TDeclaringType, int>? Count { get; protected set; }
        public FuncField<TDeclaringType, int>? ItemLength { get; protected set; }
        public Func<TDeclaringType, IEnumerable<TItem>, bool>? ShouldBreakWhen { get; protected set; }

        public Action<TDeclaringType, IEnumerable<TItem>>? Setter { get; protected set; }
        public FluentCollectionDescriptor<TDeclaringType, TItem> Into(Action<TDeclaringType, IEnumerable<TItem>> setter)
        {
            Setter = setter;
            return this;
        }
        public FluentCollectionDescriptor<TDeclaringType, TItem> WithCountOf(int count)
        {
            Count = new FuncField<TDeclaringType, int>(count);
            return this;
        }
        public FluentCollectionDescriptor<TDeclaringType, TItem> WithCountOf(Func<TDeclaringType, int> countFunc)
        {
            Count = new FuncField<TDeclaringType, int>(countFunc);
            return this;
        }

        public FluentCollectionDescriptor<TDeclaringType, TItem> BreakWhen(Func<TDeclaringType, IEnumerable<TItem>, bool> breakPredicate)
        {
            ShouldBreakWhen = breakPredicate;
            return this;
        }

        public FluentCollectionDescriptor<TDeclaringType, TItem> WithItemLengthOf(int itemLength)
        {
            ItemLength = new FuncField<TDeclaringType, int>(itemLength);
            return this;
        }
        public FluentCollectionDescriptor<TDeclaringType, TItem> WithItemLengthOf(Func<TDeclaringType, int> itemLengthFunc)
        {
            ItemLength = new FuncField<TDeclaringType, int>(itemLengthFunc);
            return this;
        }

        //TODO get rid of Try pattern? It doe snot seem to offer any advantage? Just go for throws?
        //TODO look if Deserializer(out bool success) is needed to.
        public override void Deserialize(Span<byte> bytes, TDeclaringType declaringObject, DeserializationContext deserializationContext, out int consumedLength)
        {
            if (Setter == null) throw new Exception($"{this}. Setter has not been provided!");

            consumedLength = 0;
            if (declaringObject == null) throw new ArgumentException($"{Name}. Declaring object is required for Fluent Deserialization!");

            var availableBytes = deserializationContext.Slice(bytes).Length;
            int maxAbsoluteItemOffset = deserializationContext.AbsoluteOffset + availableBytes;

            List<KeyValuePair<int, TItem>> Items = new List<KeyValuePair<int, TItem>>();

            int itemOffsetCorrection = 0;
            for (int itemNumber = 0; ; itemNumber++)
            {
                if (Count is not null && itemNumber >= Count.Get(declaringObject)) break;

                int collectionRelativeOffset = Offset?.Get(declaringObject) ?? throw new Exception($"{Name}.Neither Offset nor OffsetFunc has been set!");
                var itemRelativeOffset = collectionRelativeOffset + itemOffsetCorrection;
                var ctx = new FluentFieldContext<TDeclaringType, TItem>(deserializationContext, OffsetRelation, itemRelativeOffset, this, declaringObject);

                if (ctx.AbsoluteOffset > bytes.Length)
                    throw new Exception($"{Name}. Absolute offset of {ctx.AbsoluteOffset} is larger than dataset of {bytes.Length} bytes.");

                if (ctx.AbsoluteOffset > maxAbsoluteItemOffset)
                    throw new Exception($"{Name}. Item offset of {itemOffsetCorrection} (abs {ctx.AbsoluteOffset}) for item #{itemNumber} exceedes limits of field slice of {availableBytes} bytes.");

                if (deserializationContext.Manager.TryGetMapping<TItem>(out var deserializer) is false) throw new Exception($"{Name}. Deserializer for {typeof(TItem).FullName} not found.");

                var item = deserializer.Deserialize(bytes, ctx, out consumedLength);

                if (ItemLength is not null) consumedLength = ItemLength.Get(declaringObject);

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

        public FluentCollectionDescriptor<TDeclaringType, TItem> WithExpectedValueOf(IEnumerable<TItem> expectedvalue)
        {
            ExpectedValue = new FuncField<TDeclaringType, IEnumerable<TItem>>(expectedvalue);
            return this;
        }

        public FluentCollectionDescriptor<TDeclaringType, TItem> WithExpectedValueOf(Func<TDeclaringType, IEnumerable<TItem>> expectedValuefunc)
        {
            ExpectedValue = new FuncField<TDeclaringType, IEnumerable<TItem>>(expectedValuefunc);
            return this;
        }

        public FluentCollectionDescriptor<TDeclaringType, TItem> WithValidator(Func<TDeclaringType, IEnumerable<TItem>, bool> validateFunc)
        {
            ValidateFunc = validateFunc;
            return this;
        }
    }
}
