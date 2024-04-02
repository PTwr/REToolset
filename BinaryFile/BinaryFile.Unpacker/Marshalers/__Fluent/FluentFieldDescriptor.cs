using BinaryFile.Unpacker.Metadata;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Marshalers.__Fluent
{
    //TODO wtf is this?!
    public interface IFluentFieldContext<TDeclaringType, TItem, TImplementation> :
        IBaseFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentValidatedFieldDescriptor<TDeclaringType, TItem, TImplementation>
        where TImplementation : _BaseFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>
    {
    }

    public class FluentFieldDescriptor<TDeclaringType, TItem> :
        _BaseFluentFieldDescriptor<TDeclaringType, TItem, FluentFieldDescriptor<TDeclaringType, TItem>>,
        IFluentFieldContext<TDeclaringType, TItem, FluentFieldDescriptor<TDeclaringType, TItem>>
    {
        public FluentFieldDescriptor(string? name) : base(name)
        {
        }

        public FuncField<TDeclaringType, TItem>? ExpectedValue { get; protected set; }
        public Func<TDeclaringType, TItem, bool>? ValidateFunc { get; protected set; }

        public Action<TDeclaringType, TItem>? Setter { get; protected set; }
        public FluentFieldDescriptor<TDeclaringType, TItem> Into(Action<TDeclaringType, TItem> setter)
        {
            Setter = setter;
            return this;
        }

        public FluentFieldDescriptor<TDeclaringType, TItem> WithExpectedValueOf(TItem expectedvalue)
        {
            ExpectedValue = new FuncField<TDeclaringType, TItem>(expectedvalue);
            return this;
        }
        public FluentFieldDescriptor<TDeclaringType, TItem> WithExpectedValueOf(Func<TDeclaringType, TItem> expectedValuefunc)
        {
            ExpectedValue = new FuncField<TDeclaringType, TItem>(expectedValuefunc);
            return this;
        }

        public FluentFieldDescriptor<TDeclaringType, TItem> WithValidator(Func<TDeclaringType, TItem, bool> validateFunc)
        {
            ValidateFunc = validateFunc;
            return this;
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

        public override void Deserialize(Span<byte> bytes, TDeclaringType declaringObject, MarshalingContext deserializationContext, out int consumedLength)
        {
            if (Setter == null) throw new Exception($"{this}. Setter has not been provided!");

            consumedLength = 0;
            if (declaringObject == null) throw new ArgumentException($"{Name}. Declaring object is required for Fluent Deserialization!");

            TItem v = default!;

            int relativeOffset = Offset?.Get(declaringObject) ?? throw new Exception($"{this}. Neither Offset nor OffsetFunc has been set!");

            var ctx = new FluentFieldDeserializationContext<TDeclaringType, TItem>(deserializationContext, OffsetRelation, relativeOffset, this, declaringObject);

            if (deserializationContext.Manager.TryGetMapping<TItem>(out var deserializer) is false) throw new Exception($"{Name}. Type Mapping for {typeof(TItem).FullName} not found!");

            v = deserializer.Deserialize(bytes, ctx, out _);

            if (Length is not null) consumedLength = Length.Get(declaringObject);

            Validate(declaringObject, v);

            Setter(declaringObject, v);
        }
    }
}
