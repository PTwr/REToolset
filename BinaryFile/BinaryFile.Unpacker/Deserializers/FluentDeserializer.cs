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

        public OffsetRelation OffsetRelation { get; protected set; }
        public FuncField<TDeclaringType, int>? Offset { get; protected set; }
        public FuncField<TDeclaringType, int>? Length { get; protected set; }
        public FuncField<TDeclaringType, Encoding>? Encoding { get; protected set; }
        //TODO test!
        public FuncField<TDeclaringType, bool>? IsNestedFile { get; protected set; }
        public FuncField<TDeclaringType, bool>? NullTerminated { get; protected set; }

        public abstract bool TryDeserialize(Span<byte> bytes, TDeclaringType declaringObject, DeserializationContext deserializationContext, out int consumedLength);
    }
    public abstract class _BaseFluentFieldDescriptor<TDeclaringType, TItem, TImplementation> : _BaseFluentFieldDescriptor<TDeclaringType, TItem>, IFluentFieldDescriptor<TDeclaringType>
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
    }
    public class FluentFieldContext<TDeclaringType, TItem> : DeserializationContext
    {
        private readonly _BaseFluentFieldDescriptor<TDeclaringType, TItem> fieldDescriptor;
        private readonly TDeclaringType declaringObject;

        public override int? Length => fieldDescriptor.Length?.Get(declaringObject);
        public override Encoding? Encoding => fieldDescriptor.Encoding?.Get(declaringObject);
        public override bool? NullTerminated => fieldDescriptor.NullTerminated?.Get(declaringObject);

        public FluentFieldContext(DeserializationContext? parent, OffsetRelation offsetRelation, int relativeOffset, _BaseFluentFieldDescriptor<TDeclaringType, TItem> fieldDescriptor, TDeclaringType declaringObject)
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

            v = deserializer.Deserialize(bytes, out var success, ctx, out _);

            if (Length is not null) consumedLength = Length.Get(declaringObject);

            if (success)
            {
                Setter(declaringObject, v);

                return true;
            }
            return false;
        }
    }

    //TODO add item offset byte alignment
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

        //TODO get rid of Try pattern? It doe snot seem to offer any advantage? Just go for throws?
        //TODO look if Deserializer(out bool success) is needed to.
        public override bool TryDeserialize(Span<byte> bytes, TDeclaringType declaringObject, DeserializationContext deserializationContext, out int consumedLength)
        {
            if (Setter == null) throw new Exception($"{this}. Setter has not been provided!");

            consumedLength = 0;
            if (declaringObject == null) return false;

            var availableBytes = deserializationContext.Slice(bytes).Length;
            int maxAbsoluteItemOffset = deserializationContext.AbsoluteOffset + availableBytes;

            List<KeyValuePair<int, TItem>> Items = new List<KeyValuePair<int, TItem>>();

            int itemOffsetCorrection = 0;
            bool success = false;
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

                var item = deserializer.Deserialize(bytes, out success, ctx, out consumedLength);

                if (ItemLength is not null) consumedLength = ItemLength.Get(declaringObject);

                //TODO add option to disable this error?
                if (consumedLength <= 0) throw new Exception($"{Name}. Non-positive item consumed byte length of {consumedLength}!");
                 
                //TODO error on offset dups? but previous check should prevent dups
                Items.Add(new KeyValuePair<int, TItem>(itemOffsetCorrection, item));

                //item failed!
                if (!success) break;

                itemOffsetCorrection += consumedLength;
            }

            if (success)
            {
                Setter(declaringObject, Items.Select(i=>i.Value));

                return true;
            }

            return true;

            //int relativeOffset = Offset?.Get(declaringObject) ?? throw new Exception($"{this}. Neither Offset nor OffsetFunc has been set!");

            //var ctx = new FluentFieldContext<TDeclaringType, TItem>(deserializationContext, OffsetRelation, relativeOffset, this, declaringObject);

            //if (deserializationContext.Manager.TryGetMapping<TItem>(out var deserializer) is false) return false;

            //v = deserializer.Deserialize(bytes, out var success, ctx, out consumedLength);

            //if (success)
            //{
            //    //Setter(declaringObject, v);

            //    return true;
            //}
            //return false;
        }
    }
}
