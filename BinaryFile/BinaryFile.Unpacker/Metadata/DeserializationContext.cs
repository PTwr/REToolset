using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Metadata
{
    public class DeserializationContext
    {
        public IDeserializerManager Manager { get; protected set; }
        public DeserializationContext(DeserializationContext? parent, OffsetRelation offsetRelation, int relativeOffset, int? length)
        {
            Parent = parent;
            Length = length;

            var relation = Find(offsetRelation);

            AbsoluteOffset = relation.AbsoluteOffset + relativeOffset;

            Manager = parent?.Manager;
        }

        public DeserializationContext? Parent { get; }
        public int AbsoluteOffset { get; }
        public int? Length { get; }

        public virtual DeserializationContext Find(OffsetRelation OffsetRelation) =>
            OffsetRelation == OffsetRelation.Absolute ?
            Parent?.Find(OffsetRelation.Absolute) ?? this
            :
            OffsetRelation == OffsetRelation.Segment ? this : Parent?.Find(OffsetRelation - 1) ?? this;

        public Span<byte> Slice(Span<byte> bytes) => Length.HasValue ? bytes.Slice(AbsoluteOffset, Length.Value) : bytes.Slice(AbsoluteOffset);
    }

    public class RootDataOffset : DeserializationContext
    {
        public RootDataOffset(IDeserializerManager manager) : base(null, OffsetRelation.Absolute, 0, null)
        {
            Manager = manager;
        }

        public override DeserializationContext Find(OffsetRelation OffsetRelation) =>
            OffsetRelation is OffsetRelation.Absolute or OffsetRelation.Segment ? this :
            throw new ArgumentException($"Looking for ancestor DataOffset of Root. Remaining OffsetRelation = {OffsetRelation}");
    }

    public class NestedFileDataOffset : DeserializationContext
    {
        public NestedFileDataOffset(DeserializationContext? parent, OffsetRelation offsetRelation, int absoluteOffset, int length) 
            : base(parent, offsetRelation, absoluteOffset, length)
        {
        }

        //short circuit Absolute for nested file
        public override DeserializationContext Find(OffsetRelation OffsetRelation) =>
            OffsetRelation is OffsetRelation.Absolute or OffsetRelation.Segment ? this : 
                throw new ArgumentException($"Looking for ancestor DataOffset of NestedFile. Remaining OffsetRelation = {OffsetRelation}")
            ;
    }
}
