using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Metadata
{
    public interface IMarshalingContext : IWithCommonMarshalingMetadata
    {
        string? Name { get; }
        int AbsoluteOffset { get; }
        IDeserializerManager DeserializerManager { get; }
        ISerializerManager SerializerManager { get; }
        IMarshalingContext? Parent { get; }

        IMarshalingContext Find(OffsetRelation offsetRelation);
        Span<byte> Slice(Span<byte> bytes);
    }

    //TODO switch all interfaces to generic DeserializationContext! EVerythings generic! No typeless objects to deal with :D
    public class MarshalingContext : IMarshalingContext
    {
        public string? Name { get; protected set; }

        public override string ToString()
        {
            return $"{Name} offset={AbsoluteOffset} length={Length}";
        }

        public IDeserializerManager DeserializerManager { get; protected set; }
        public ISerializerManager SerializerManager { get; protected set; }
        public MarshalingContext(IMarshalingContext? parent, OffsetRelation offsetRelation, int relativeOffset)
        {
            Parent = parent;

            var relation = Parent?.Find(offsetRelation) ?? this;

            AbsoluteOffset = relation.AbsoluteOffset + relativeOffset;

            DeserializerManager = parent?.DeserializerManager!;
            SerializerManager = parent?.SerializerManager!;
        }

        public IMarshalingContext? Parent { get; }
        public int AbsoluteOffset { get; }

        //metadata accessors
        public virtual int? Length { get; }
        public virtual int? ItemLength { get; }
        public virtual int? Count { get; }
        public virtual Encoding? Encoding { get; }
        public virtual bool? NullTerminated { get; }
        public virtual bool? LittleEndian { get; }
        public virtual bool? IsNestedFile { get; }

        public virtual IMarshalingContext Find(OffsetRelation offsetRelation) =>
            offsetRelation == OffsetRelation.Absolute ?
            Parent?.Find(OffsetRelation.Absolute) ?? this
            :
            offsetRelation == OffsetRelation.Segment ? this : Parent?.Find(offsetRelation - 1) ?? this;

        public Span<byte> Slice(Span<byte> bytes) => Length.HasValue ? bytes.Slice(AbsoluteOffset, Length.Value) : bytes.Slice(AbsoluteOffset);
    }

    public class RootMarshalingContext : MarshalingContext
    {
        public RootMarshalingContext(IDeserializerManager deserializerManager, ISerializerManager serializerManager) : base(null, OffsetRelation.Absolute, 0)
        {
            SerializerManager = serializerManager;
            DeserializerManager = deserializerManager;
        }

        public override MarshalingContext Find(OffsetRelation offsetRelation) =>
            offsetRelation is OffsetRelation.Absolute or OffsetRelation.Segment ? this :
            throw new ArgumentException($"Looking for ancestor DataOffset of Root. Remaining OffsetRelation = {offsetRelation}");
    }
}
