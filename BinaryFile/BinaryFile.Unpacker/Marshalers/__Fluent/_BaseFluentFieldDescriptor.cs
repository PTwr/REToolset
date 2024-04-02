using BinaryFile.Unpacker.Metadata;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Marshalers.__Fluent
{
    public abstract class _BaseFluentFieldDescriptor<TDeclaringType, TItem>
    {
        public string? Name { get; protected set; }
        public _BaseFluentFieldDescriptor(string? name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name ?? base.ToString()!;
        }

        //TODO I hate this, figure out nicer descriptor->context->deserializer metadata passthrough
        public OffsetRelation OffsetRelation { get; protected set; }
        public FuncField<TDeclaringType, int>? Offset { get; protected set; }
        public FuncField<TDeclaringType, int>? Length { get; protected set; }
        public FuncField<TDeclaringType, Encoding>? Encoding { get; protected set; }
        //TODO test!
        public FuncField<TDeclaringType, bool>? IsNestedFile { get; protected set; }
        public FuncField<TDeclaringType, bool>? NullTerminated { get; protected set; }
        public FuncField<TDeclaringType, bool>? LittleEndian { get; protected set; }

        public abstract void Deserialize(Span<byte> bytes, TDeclaringType declaringObject, MarshalingContext deserializationContext, out int consumedLength);
    }

    public interface IBaseFluentFieldDescriptor<TDeclaringType, TItem, TImplementation> :
        IFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentStringFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentIntegerFieldDescriptor<TDeclaringType, TItem, TImplementation>
        where TImplementation : _BaseFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>
    {
    }

    public abstract class _BaseFluentFieldDescriptor<TDeclaringType, TItem, TImplementation> :
        _BaseFluentFieldDescriptor<TDeclaringType, TItem>,
        IBaseFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>
        where TImplementation : _BaseFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>
    {
        protected _BaseFluentFieldDescriptor(string? name) : base(name)
        {
        }

        public TImplementation AtOffset(int offset, OffsetRelation offsetRelation = OffsetRelation.Segment)
        {
            OffsetRelation = offsetRelation;
            Offset = new FuncField<TDeclaringType, int>(offset);

            return (TImplementation)this;
        }
        public TImplementation AtOffset(Func<TDeclaringType, int> offsetFunc, OffsetRelation offsetRelation = OffsetRelation.Segment)
        {
            OffsetRelation = offsetRelation;
            Offset = new FuncField<TDeclaringType, int>(offsetFunc);

            return (TImplementation)this;
        }

        public TImplementation WithLengthOf(int length)
        {
            Length = new FuncField<TDeclaringType, int>(length);

            return (TImplementation)this;
        }
        public TImplementation WithLengthOf(Func<TDeclaringType, int> lengthFunc)
        {
            Length = new FuncField<TDeclaringType, int>(lengthFunc);

            return (TImplementation)this;
        }

        public TImplementation WithEncoding(Encoding encoding)
        {
            Encoding = new FuncField<TDeclaringType, Encoding>(encoding);

            return (TImplementation)this;
        }
        public TImplementation WithEncoding(Func<TDeclaringType, Encoding> encodingFunc)
        {
            Encoding = new FuncField<TDeclaringType, Encoding>(encodingFunc);

            return (TImplementation)this;
        }

        public TImplementation AsNestedFile(bool isNestedFile = true)
        {
            IsNestedFile = new FuncField<TDeclaringType, bool>(isNestedFile);

            return (TImplementation)this;
        }
        public TImplementation AsNestedFile(Func<TDeclaringType, bool> isNestedFileFunc)
        {
            IsNestedFile = new FuncField<TDeclaringType, bool>(isNestedFileFunc);

            return (TImplementation)this;
        }

        public TImplementation WithNullTerminator(bool isNullTerminated = true)
        {
            NullTerminated = new FuncField<TDeclaringType, bool>(isNullTerminated);

            return (TImplementation)this;
        }
        public TImplementation WithNullTerminator(Func<TDeclaringType, bool> isNullTerminatedFunc)
        {
            NullTerminated = new FuncField<TDeclaringType, bool>(isNullTerminatedFunc);

            return (TImplementation)this;
        }

        public TImplementation InLittleEndian(bool inLittleEndian = true)
        {
            LittleEndian = new FuncField<TDeclaringType, bool>(inLittleEndian);

            return (TImplementation)this;
        }
        public TImplementation InLittleEndian(Func<TDeclaringType, bool> inLittleEndianFunc)
        {
            LittleEndian = new FuncField<TDeclaringType, bool>(inLittleEndianFunc);

            return (TImplementation)this;
        }
    }
}
