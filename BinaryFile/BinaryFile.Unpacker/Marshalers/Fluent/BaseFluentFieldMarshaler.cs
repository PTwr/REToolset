using BinaryFile.Unpacker.Metadata;
using ReflectionHelper;
using System.Text;

namespace BinaryFile.Unpacker.Marshalers.Fluent
{
    public abstract class BaseFluentFieldMarshaler<TDeclaringType, TItem, TImplementation> :
        IFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentIntegerFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentStringFieldDescriptor<TDeclaringType, TItem, TImplementation>
        where TImplementation : BaseFluentFieldMarshaler<TDeclaringType, TItem, TImplementation>
    {
        public bool DeserializationInitialized { get; protected set; }
        public bool SerializationInitialized { get; protected set; }
        protected string? Name;

        public BaseFluentFieldMarshaler(string name)
        {
            Name = name;
        }

        protected OffsetRelation OffsetRelation;
        protected FuncField<TDeclaringType, int>? Offset;

        //TODO this public is ugly
        public FuncField<TDeclaringType, int>? Order { get; protected set; }
        public FuncField<TDeclaringType, int>? DeserializationOrder { get; protected set; }
        public FuncField<TDeclaringType, int>? SerializationOrder { get; protected set; }
        protected DynamicCommonMarshalingMetadata<TDeclaringType, TItem> Metadata { get; set; } = new();
        protected FuncField<TDeclaringType, bool>? WhenSimple { get; set; }
        protected FuncField<TDeclaringType, bool>? DeserializationWhenSimple { get; set; }
        protected FuncField<TDeclaringType, bool>? SerializationWhenSimple { get; set; }
        protected PatternDelegate? WhenAdvanced { get; set; }

        public delegate bool PatternDelegate(TDeclaringType obj, Span<byte> data, IMarshalingContext ctx);

        public TImplementation WithPatternCondition(PatternDelegate predicate)
        {
            WhenAdvanced = predicate;
            return (TImplementation)this;
        }
        //TODO FuncField can be replaced by predicate returning static, so predicate+static overloads are not strictly necessary
        //TODO but meta like Offset or ExpectedValue will usually be used with static values so it would be confusing and wasteful
        //TODO to not have value overloads, but for fields like Conditional Deserialization it makes no sense to have static value
        public TImplementation WhenFlag(Func<TDeclaringType, bool> predicate)
        {
            WhenSimple = new FuncField<TDeclaringType, bool>(predicate);
            return (TImplementation)this;
        }
        public TImplementation WhenFlag(bool staticValue)
        {
            WhenSimple = new FuncField<TDeclaringType, bool>(staticValue);
            return (TImplementation)this;
        }
        public TImplementation DeserializeWhenFlag(Func<TDeclaringType, bool> predicate)
        {
            DeserializationWhenSimple = new FuncField<TDeclaringType, bool>(predicate);
            return (TImplementation)this;
        }
        public TImplementation DeserializeWhenFlag(bool staticValue)
        {
            DeserializationWhenSimple = new FuncField<TDeclaringType, bool>(staticValue);
            return (TImplementation)this;
        }
        public TImplementation SerializeWhenFlag(Func<TDeclaringType, bool> predicate)
        {
            SerializationWhenSimple = new FuncField<TDeclaringType, bool>(predicate);
            return (TImplementation)this;
        }
        public TImplementation SerializeWhenFlag(bool staticValue)
        {
            SerializationWhenSimple = new FuncField<TDeclaringType, bool>(staticValue);
            return (TImplementation)this;
        }

        public TImplementation AsNestedFile(bool isNestedFile = true)
        {
            Metadata.IsNestedFile = new FuncField<TDeclaringType, bool>(isNestedFile);
            return (TImplementation)this;
        }

        public TImplementation AsNestedFile(Func<TDeclaringType, bool> isNestedFileFunc)
        {
            Metadata.IsNestedFile = new FuncField<TDeclaringType, bool>(isNestedFileFunc);
            return (TImplementation)this;
        }

        public TImplementation AtOffset(Func<TDeclaringType, int> offsetFunc, OffsetRelation offsetRelation = OffsetRelation.Segment)
        {
            OffsetRelation = offsetRelation;
            Offset = new FuncField<TDeclaringType, int>(offsetFunc);
            return (TImplementation)this;
        }

        public TImplementation AtOffset(int offset, OffsetRelation offsetRelation = OffsetRelation.Segment)
        {
            OffsetRelation = offsetRelation;
            Offset = new FuncField<TDeclaringType, int>(offset);
            return (TImplementation)this;
        }

        public TImplementation InLittleEndian(bool inLittleEndian = true)
        {
            Metadata.LittleEndian = new FuncField<TDeclaringType, bool>(inLittleEndian);
            return (TImplementation)this;
        }

        public TImplementation InLittleEndian(Func<TDeclaringType, bool> inLittleEndianFunc)
        {
            Metadata.LittleEndian = new FuncField<TDeclaringType, bool>(inLittleEndianFunc);
            return (TImplementation)this;
        }

        public TImplementation WithEncoding(Encoding encoding)
        {
            Metadata.Encoding = new FuncField<TDeclaringType, Encoding>(encoding);
            return (TImplementation)this;
        }

        public TImplementation WithEncoding(Func<TDeclaringType, Encoding> encodingFunc)
        {
            Metadata.Encoding = new FuncField<TDeclaringType, Encoding>(encodingFunc);
            return (TImplementation)this;
        }

        public TImplementation WithLengthOf(Func<TDeclaringType, int> lengthFunc)
        {
            Metadata.Length = new FuncField<TDeclaringType, int>(lengthFunc);
            return (TImplementation)this;
        }

        public TImplementation WithLengthOf(int length)
        {
            Metadata.Length = new FuncField<TDeclaringType, int>(length);
            return (TImplementation)this;
        }

        public TImplementation WithNullTerminator(bool isNullTerminated = true)
        {
            Metadata.NullTerminated = new FuncField<TDeclaringType, bool>(isNullTerminated);
            return (TImplementation)this;
        }

        public TImplementation WithNullTerminator(Func<TDeclaringType, bool> isNullTerminatedFunc)
        {
            Metadata.NullTerminated = new FuncField<TDeclaringType, bool>(isNullTerminatedFunc);
            return (TImplementation)this;
        }

        public TImplementation InOrder(int order)
        {
            Order = new FuncField<TDeclaringType, int>(order);
            return (TImplementation)this;
        }

        public TImplementation InOrder(Func<TDeclaringType, int> orderFunc)
        {
            Order = new FuncField<TDeclaringType, int>(orderFunc);
            return (TImplementation)this;
        }

        public TImplementation InDeserializationOrder(int order)
        {
            DeserializationOrder = new FuncField<TDeclaringType, int>(order);
            return (TImplementation)this;
        }

        public TImplementation InDeserializationOrder(Func<TDeclaringType, int> orderFunc)
        {
            DeserializationOrder = new FuncField<TDeclaringType, int>(orderFunc);
            return (TImplementation)this;
        }

        public TImplementation InSerializationOrder(int order)
        {
            SerializationOrder = new FuncField<TDeclaringType, int>(order);
            return (TImplementation)this;
        }

        public TImplementation InSerializationOrder(Func<TDeclaringType, int> orderFunc)
        {
            SerializationOrder = new FuncField<TDeclaringType, int>(orderFunc);
            return (TImplementation)this;
        }
    }
}
