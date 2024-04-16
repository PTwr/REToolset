using BinaryFile.Unpacker.New;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
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
        int OffsetCorrection { get; }

        IMarshalingContext Find(OffsetRelation offsetRelation);
        Span<byte> Slice(Span<byte> bytes);

        TType Activate<TType>();
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
        public MarshalingContext(IMarshalingContext? parent, OffsetRelation offsetRelation, int relativeOffset, int offsetCorrection = 0)
        {
            Parent = parent;
            OffsetCorrection = offsetCorrection;
            var relation = Parent?.Find(offsetRelation) ?? this;

            OffsetCorrection = (parent?.OffsetCorrection ?? 0) + offsetCorrection;
            OffsetCorrection = offsetCorrection;
            AbsoluteOffset = relation.AbsoluteOffset + relativeOffset + OffsetCorrection;

            DeserializerManager = parent?.DeserializerManager!;
            SerializerManager = parent?.SerializerManager!;
        }

        public IMarshalingContext? Parent { get; }
        public int OffsetCorrection { get; }
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

        public Span<byte> Slice(Span<byte> bytes)
        {
            if (AbsoluteOffset > bytes.Length)
                throw new Exception($"{Name}. AbbsoluteOffset of '{AbsoluteOffset}' is out of bounds of dataset of {bytes.Length} bytes!");

            if (Length.HasValue && AbsoluteOffset + Length.Value - OffsetCorrection > bytes.Length)
                throw new Exception($"{Name}. Slice of {Length.Value - OffsetCorrection > bytes.Length} from {AbsoluteOffset} reaches out of bounds of dataset of {bytes.Length} bytes!");

            if (Length.HasValue && Length.Value - OffsetCorrection < 0)
                throw new Exception($"{Name}. Negative corrected slice length! {Length.Value} - {OffsetCorrection} = {Length.Value - OffsetCorrection}! Something is probably wrong in Length and/or ItemLength!");

            return Length.HasValue ? bytes.Slice(AbsoluteOffset, Length.Value - OffsetCorrection) : bytes.Slice(AbsoluteOffset);
        }

        public virtual TType Activate<TType>()
        {
            return Activator.CreateInstance<TType>();
        }
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
