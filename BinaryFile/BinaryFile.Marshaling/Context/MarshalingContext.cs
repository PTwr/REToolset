using BinaryFile.Marshaling.Common;
using BinaryFile.Marshaling.MarshalingStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.Context
{
    public class RootMarshalingContext : MarshalingContext
    {
        public RootMarshalingContext(IMarshalerStore marshalerStore)
            : base("rootCtx", marshalerStore, null, 0, OffsetRelation.Absolute, null)
        {

        }
    }
    public class MarshalingContext : IMarshalingContext
    {
        private readonly int relativeOffset;

        public MarshalingContext(string fieldName, IMarshalerStore marshalerStore, IMarshalingContext? parent, int relativeOffset, OffsetRelation offsetRelation, IMarshalingMetadata? marshalingMetadata)
        {
            FieldName = fieldName;
            MarshalerStore = marshalerStore;
            Parent = parent;
            this.relativeOffset = relativeOffset;
            OffsetRelation = offsetRelation;
            Metadata = marshalingMetadata;
            FieldAbsoluteOffset = FindRelation(offsetRelation).ItemAbsoluteOffset + relativeOffset;
        }

        public string FieldName { get; protected set; }
        public IMarshalerStore MarshalerStore { get; }
        public IMarshalingContext? Parent { get; }
        public OffsetRelation OffsetRelation { get; protected set; }
        public IMarshalingMetadata? Metadata { get; protected set; }
        public int FieldAbsoluteOffset { get; protected set; }
        public int ItemAbsoluteOffset => FieldAbsoluteOffset + (ItemOffset ?? 0);
        public int? ItemOffset { get; protected set; }

        public int? FieldLength { get; protected set; }
        public int? ItemLength { get; protected set; }
        public IMarshalingContext WithFieldByteLength(int? byteLength)
        {
            //pass fieldByteLength to down the hierarchy
            if (byteLength is null && OffsetRelation == OffsetRelation.Segment)
            {
                var relationFieldLength = Parent?.FieldLength;
                if (relationFieldLength is not null)
                {
                    //but correct for
                    byteLength = relationFieldLength 
                        //offset inside of segment
                        - this.relativeOffset
                        //and array position
                        - (Parent?.ItemOffset ?? 0);
                }
            }

            FieldLength = byteLength;
            return this;
        }
        public IMarshalingContext WithItemByteLength(int? itemByteLength)
        {
            ItemLength = itemByteLength;
            return this;
        }

        public void WithItemOffset(int itemOffset)
        {
            ItemOffset = itemOffset;
        }

        public Memory<byte> ItemSlice(Memory<byte> source)
        {
            if (FieldAbsoluteOffset >= source.Length)
                throw new Exception($"Error when slicing for {FieldName}. Field Absolute Offset of {FieldAbsoluteOffset} exceedes available length of {source.Length}");

            var slice = source.Slice(FieldAbsoluteOffset);
            if (FieldLength.HasValue)
            {
                if (FieldLength.Value > slice.Length)
                    throw new Exception($"Error when slicing for {FieldName}. Field Length of {FieldLength.Value} exceedes available length of {slice.Length}");
                slice = slice.Slice(0, FieldLength.Value);
            }

            if (ItemOffset.HasValue)
            {
                if (ItemOffset.Value > slice.Length)
                    throw new Exception($"Error when slicing for {FieldName}. Item Offset of {ItemOffset.Value} exceedes available length of {slice.Length}");
                slice = slice.Slice(ItemOffset.Value);
            }

            if (ItemLength.HasValue)
            {
                if (ItemLength.Value > slice.Length)
                    throw new Exception($"Error when slicing for {FieldName}. Item Length of {ItemLength.Value} exceedes available length of {slice.Length}");
                slice.Slice(0, ItemLength.Value);
            }

            return slice;
        }

        public virtual IMarshalingContext FindRelation(OffsetRelation offsetRelation) =>
            offsetRelation == OffsetRelation.Absolute ?
            Parent?.FindRelation(OffsetRelation.Absolute) ?? this
            :
            offsetRelation == OffsetRelation.Segment ? Parent ?? this : Parent?.FindRelation(offsetRelation - 1) ?? this;
    }
}
