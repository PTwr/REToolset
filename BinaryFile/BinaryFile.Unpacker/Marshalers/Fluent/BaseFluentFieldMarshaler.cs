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
        protected string? Name;

        public BaseFluentFieldMarshaler(string name)
        {
            Name = name;
        }

        protected OffsetRelation OffsetRelation;
        protected FuncField<TDeclaringType, int>? Offset;
        protected DynamicCommonMarshalingMetadata<TDeclaringType> Metadata { get; set; } = new DynamicCommonMarshalingMetadata<TDeclaringType>();

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
    }
}
