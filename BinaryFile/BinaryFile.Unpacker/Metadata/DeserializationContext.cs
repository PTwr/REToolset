using BinaryFile.Unpacker.Deserializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Metadata
{
    public class FieldContext<TDeclaringType, TFieldType> : DeserializationContext
        where TDeclaringType : class
    {
        private readonly FieldDescriptor<TDeclaringType, TFieldType> fieldDescriptor;
        private readonly TDeclaringType declaringObject;

        public override int? Length => fieldDescriptor.Length?.Get(declaringObject);
        public override int? Count => fieldDescriptor.Count?.Get(declaringObject);
        public override Encoding? Encoding => fieldDescriptor.Encoding.Get(declaringObject);
        //TODO figure out how to pass current collection here, maybe separate context for collections?
        //public override bool ShouldBreakWhen => fieldDescriptor.ShouldBreakWhen(declaringObject);

        public FieldContext(DeserializationContext? parent, OffsetRelation offsetRelation, int relativeOffset, FieldDescriptor<TDeclaringType, TFieldType> fieldDescriptor, TDeclaringType declaringObject)
            : base(parent, offsetRelation, relativeOffset)
        {
            this.fieldDescriptor = fieldDescriptor;
            this.declaringObject = declaringObject;
        }

        //short circuit Absolute for nested file
        public override DeserializationContext Find(OffsetRelation offsetRelation) => fieldDescriptor.IsNestedFile ? (
            offsetRelation is OffsetRelation.Absolute or OffsetRelation.Segment ? this :
                throw new ArgumentException($"Looking for ancestor DataOffset of NestedFile. Remaining OffsetRelation = {offsetRelation}"))
            : base.Find(offsetRelation);
    }

    public class DeserializationContext
    {
        public string? Name;

        public override string ToString()
        {
            return $"{Name} offset={AbsoluteOffset} length={Length}";
        }

        public IDeserializerManager Manager { get; protected set; }
        public DeserializationContext(DeserializationContext? parent, OffsetRelation offsetRelation, int relativeOffset)
        {
            Parent = parent;

            var relation = Parent?.Find(offsetRelation) ?? this;

            AbsoluteOffset = relation.AbsoluteOffset + relativeOffset;

            Manager = parent?.Manager!;
        }

        public DeserializationContext? Parent { get; }
        public int AbsoluteOffset { get; }
        public virtual int? Length { get; }
        public virtual int? Count { get; }
        public virtual Encoding? Encoding { get; }
        public virtual bool ShouldContinueCollection { get; }

        public virtual DeserializationContext Find(OffsetRelation offsetRelation) =>
            offsetRelation == OffsetRelation.Absolute ?
            Parent?.Find(OffsetRelation.Absolute) ?? this
            :
            offsetRelation == OffsetRelation.Segment ? this : Parent?.Find(offsetRelation - 1) ?? this;

        public Span<byte> Slice(Span<byte> bytes) => Length.HasValue ? bytes.Slice(AbsoluteOffset, Length.Value) : bytes.Slice(AbsoluteOffset);
    }

    public class RootDataOffset : DeserializationContext
    {
        public RootDataOffset(IDeserializerManager manager) : base(null, OffsetRelation.Absolute, 0)
        {
            Manager = manager;
        }

        public override DeserializationContext Find(OffsetRelation offsetRelation) =>
            offsetRelation is OffsetRelation.Absolute or OffsetRelation.Segment ? this :
            throw new ArgumentException($"Looking for ancestor DataOffset of Root. Remaining OffsetRelation = {offsetRelation}");
    }
}
