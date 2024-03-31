using BinaryFile.Unpacker.Metadata;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BinaryFile.Unpacker.Deserializers
{
    public class FluentDeserializer<TDeclaringType> : SerializedAndDeserializerBase<TDeclaringType>, IDeserializer<TDeclaringType>
    {
        List<IFluentFieldDescriptor<TDeclaringType>> Descriptors = new List<IFluentFieldDescriptor<TDeclaringType>>();

        public TDeclaringType Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext, out int consumedLength)
        {
            consumedLength = 0;
            success = false;
            //TODO safety checks
            var result = (TDeclaringType)Activator.CreateInstance(MappedType);

            foreach (var descriptor in Descriptors)
            {
                descriptor.TryDeserialize(data, result, deserializationContext, out consumedLength);
            }

            success = true;
            return result;
        }

        public FluentFieldDescriptor<TDeclaringType, TItem> WithField<TItem>(string? name = null)
        {
            var descriptor = new FluentFieldDescriptor<TDeclaringType, TItem>(name);
            Descriptors.Add(descriptor);
            return descriptor;
        }

        public FluentCollectionDescriptor<TDeclaringType, TItem> WithCollectionOf<TItem>(string? name = null)
        {
            var descriptor = new FluentCollectionDescriptor<TDeclaringType, TItem>(name);
            Descriptors.Add(descriptor);
            return descriptor;
        }
    }

    public interface IFluentFieldDescriptor<TDeclaringType>
    {
        bool TryDeserialize(Span<byte> bytes, TDeclaringType declaringObject, DeserializationContext deserializationContext, out int consumedLength);
    }
    public abstract class _BaseFluentFieldDescriptor<TDeclaringType, TItem, TImplementation> : IFluentFieldDescriptor<TDeclaringType>
        where TImplementation : _BaseFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>
    {
        public _BaseFluentFieldDescriptor(string? name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name ?? base.ToString()!;
        }

        public OffsetRelation OffsetRelation { get; protected set; }
        public FuncField<TDeclaringType, int>? Offset { get; protected set; }
        public FuncField<TDeclaringType, int>? Length { get; protected set; }
        public FuncField<TDeclaringType, Encoding>? Encoding { get; protected set; }
        public FuncField<TDeclaringType, bool>? IsNestedFile { get; protected set; }
        public string? Name { get; protected set; }

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

        public TImplementation AsNestedFile(bool isNestedFile)
        {
            IsNestedFile = new FuncField<TDeclaringType, bool>(isNestedFile);

            return (TImplementation)this;
        }
        public TImplementation AsNestedFile(Func<TDeclaringType, bool> isNestedFileFunc)
        {
            IsNestedFile = new FuncField<TDeclaringType, bool>(isNestedFileFunc);

            return (TImplementation)this;
        }

        public abstract bool TryDeserialize(Span<byte> bytes, TDeclaringType declaringObject, DeserializationContext deserializationContext, out int consumedLength);
    }
    public class FluentFieldContext<TDeclaringType, TItem> : DeserializationContext
    {
        private readonly FluentFieldDescriptor<TDeclaringType, TItem> fieldDescriptor;
        private readonly TDeclaringType declaringObject;

        public override int? Length => fieldDescriptor.Length?.Get(declaringObject);
        public override Encoding? Encoding => fieldDescriptor.Encoding?.Get(declaringObject);

        public FluentFieldContext(DeserializationContext? parent, OffsetRelation offsetRelation, int relativeOffset, FluentFieldDescriptor<TDeclaringType, TItem> fieldDescriptor, TDeclaringType declaringObject)
            : base(parent, offsetRelation, relativeOffset)
        {
            this.fieldDescriptor = fieldDescriptor;
            this.declaringObject = declaringObject;
        }

        //short circuit Absolute for nested file
        //TODO clean rewrite, this is fugly :D
        public override DeserializationContext Find(OffsetRelation offsetRelation) => (fieldDescriptor.IsNestedFile?.Get(declaringObject) ?? false) ? (
            offsetRelation is OffsetRelation.Absolute or OffsetRelation.Segment ? this :
                throw new ArgumentException($"{fieldDescriptor}. Looking for ancestor DataOffset of NestedFile. Remaining OffsetRelation = {offsetRelation}"))
            : base.Find(offsetRelation);
    }
    public class FluentFieldDescriptor<TDeclaringType, TItem> : _BaseFluentFieldDescriptor<TDeclaringType, TItem, FluentFieldDescriptor<TDeclaringType, TItem>>
    {
        public FluentFieldDescriptor(string? name) : base(name)
        {
        }

        public Action<TDeclaringType, TItem>? Setter { get; protected set; }
        public FluentFieldDescriptor<TDeclaringType, TItem> Into(Action<TDeclaringType, TItem> setter)
        {
            Setter = setter;
            return this;
        }

        public override bool TryDeserialize(Span<byte> bytes, TDeclaringType declaringObject, DeserializationContext deserializationContext, out int consumedLength)
        {
            if (Setter == null) throw new Exception($"{this}. Setter has not been provided!");

            consumedLength = 0;
            if (declaringObject == null) return false;

            TItem v = default!;

            int relativeOffset = Offset?.Get(declaringObject) ?? throw new Exception($"{this}. Neither Offset nor OffsetFunc has been set!");

            var ctx = new FluentFieldContext<TDeclaringType, TItem>(deserializationContext, OffsetRelation, relativeOffset, this, declaringObject);

            if (deserializationContext.Manager.TryGetMapping<TItem>(out var deserializer) is false) return false;

            v = deserializer.Deserialize(bytes, out var success, ctx, out consumedLength);

            if (success)
            {
                Setter(declaringObject, v);

                return true;
            }
            return false;
        }
    }
    public class FluentCollectionDescriptor<TDeclaringType, TItem> : _BaseFluentFieldDescriptor<TDeclaringType, TItem, FluentCollectionDescriptor<TDeclaringType, TItem>>
    {
        public FluentCollectionDescriptor(string? name) : base(name)
        {
        }

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

        public override bool TryDeserialize(Span<byte> bytes, TDeclaringType declaringObject, DeserializationContext deserializationContext, out int consumedLength)
        {
            throw new NotImplementedException();
        }
    }
}
