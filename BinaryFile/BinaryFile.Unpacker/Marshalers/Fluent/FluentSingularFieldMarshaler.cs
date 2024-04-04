using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BinaryFile.Unpacker.Marshalers.Fluent
{
    public class FluentSingularFieldMarshaler<TDeclaringType, TItem> :
        BaseFluentFieldMarshaler<TDeclaringType, TItem, FluentSingularFieldMarshaler<TDeclaringType, TItem>>,
        IFieldMarshaler<TDeclaringType>,
        IFluentSingularFieldMarshaler<TDeclaringType, TItem, FluentSingularFieldMarshaler<TDeclaringType, TItem>>
    {
        public FluentSingularFieldMarshaler(string name) : base(name)
        {
        }

        protected Action<TDeclaringType, TItem>? Setter { get; set; }
        protected Func<TDeclaringType, TItem>? Getter { get; set; }

        protected FuncField<TDeclaringType, TItem>? ExpectedValue { get; set; }
        protected Func<TDeclaringType, TItem, bool>? ValidateFunc { get; set; }

        public void Deserialize(TDeclaringType declaringObject, Span<byte> bytes, IMarshalingContext context, out int consumedLength)
        {
            if (Setter == null) throw new Exception($"{this}. Setter has not been provided!");

            consumedLength = 0;
            if (declaringObject == null) throw new ArgumentException($"{Name}. Declaring object is required for Fluent Deserialization!");

            TItem v = default!;

            int relativeOffset = Offset?.Get(declaringObject) ?? throw new Exception($"{this}. Neither Offset nor OffsetFunc has been set!");

            var fieldContext = new FluentMarshalingContext<TDeclaringType, TItem>(Name, context, OffsetRelation, relativeOffset, Metadata, declaringObject);

            //TODO implement conditional implementation switcher (needed for U8(arc) file/directory handling)
            if (context.DeserializerManager.TryGetMapping<TItem>(out var deserializer) is false) throw new Exception($"{Name}. Type Mapping for {typeof(TItem).FullName} not found!");

            v = deserializer.Deserialize(bytes, fieldContext, out _);

            if (Metadata.Length is not null) consumedLength = Metadata.Length.Get(declaringObject);

            Validate(declaringObject, v);

            Setter(declaringObject, v);
        }

        public void Serialize(TDeclaringType declaringObject, ByteBuffer buffer, IMarshalingContext context, out int consumedLength)
        {
            consumedLength = 0;
            if (Getter == null) throw new Exception($"{Name}. Getter has not been provided!");

            var v = Getter(declaringObject);

            //TODO implement conditional (de)serialization :D
            if (v == null) throw new Exception($"{Name}. Field value is null! Check for errors and consider using conditional serialization!");

            int relativeOffset = Offset?.Get(declaringObject) ?? throw new Exception($"{this}. Neither Offset nor OffsetFunc has been set!");

            var fieldContext = new FluentMarshalingContext<TDeclaringType, TItem>(Name, context, OffsetRelation, relativeOffset, Metadata, declaringObject);

            if (context.SerializerManager.TryGetMapping<TItem>(out var serializer) is false) throw new Exception($"{Name}. Type Mapping for {typeof(TItem).FullName} not found!");

            serializer.Serialize(v, buffer, fieldContext, out consumedLength);
        }

        //TODO rewrite! fugly!
        public void Validate(TDeclaringType declaringObject, TItem value)
        {
            if (ExpectedValue is not null)
            {
                var expectedVal = ExpectedValue.Get(declaringObject);
                var result = EqualityComparer<TItem>.Default.Equals(value, expectedVal);
                if (!result) throw new Exception($"{Name}. Unexpected Value! Expected: '{expectedVal}', actual: '{value}'");
            }
            if (ValidateFunc is not null)
            {
                var result = ValidateFunc.Invoke(declaringObject, value);
                if (!result) throw new Exception($"{Name}. Validation failed! Deserialized value: '{value}'");
            }
        }

        public FluentSingularFieldMarshaler<TDeclaringType, TItem> From(Func<TDeclaringType, TItem> getter)
        {
            Getter = getter;
            return this;
        }

        public FluentSingularFieldMarshaler<TDeclaringType, TItem> Into(Action<TDeclaringType, TItem> setter)
        {
            Setter = setter;
            return this;
        }

        public FluentSingularFieldMarshaler<TDeclaringType, TItem> WithExpectedValueOf(TItem expectedValue)
        {
            ExpectedValue = new FuncField<TDeclaringType, TItem>(expectedValue);
            return this;
        }

        public FluentSingularFieldMarshaler<TDeclaringType, TItem> WithExpectedValueOf(Func<TDeclaringType, TItem> expectedValuefunc)
        {
            ExpectedValue = new FuncField<TDeclaringType, TItem>(expectedValuefunc);
            return this;
        }

        public FluentSingularFieldMarshaler<TDeclaringType, TItem> WithValidator(Func<TDeclaringType, TItem, bool> validateFunc)
        {
            ValidateFunc = validateFunc;
            return this;
        }
    }
}
