using BinaryFile.Unpacker.Deserializers;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Metadata
{
    public interface IDeserializationContext
    {
        int AbsoluteOffset { get; }
        int? Count { get; }
        Encoding? Encoding { get; }
        int? Length { get; }
        bool? LittleEndian { get; }
        IDeserializerManager Manager { get; }
        bool? NullTerminated { get; }
        DeserializationContext? Parent { get; }

        DeserializationContext Find(OffsetRelation offsetRelation);
        Span<byte> Slice(Span<byte> bytes);
        string ToString();
    }

    //TODO switch all interfaces to generic DeserializationContext! EVerythings generic! No typeless objects to deal with :D
    public class DeserializationContext : IDeserializationContext
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
        public virtual bool? NullTerminated { get; }
        public virtual bool? LittleEndian { get; }

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
