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

        public void Deserialize(TDeclaringType declaringObject, Span<byte> bytes, IMarshalingContext deserializationContext, out int consumedLength)
        {
            throw new NotImplementedException();
        }

        public void Serialize(TDeclaringType declaringObject, ByteBuffer buffer, ISerializationContext serializationContext, out int consumedLength)
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
