using BinaryFile.Marshaling.Common;
using BinaryFile.Marshaling.MarshalingStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.Context
{
    public class MarshalingContext : IMarshalingContext
    {
        public MarshalingContext(string fieldName, IMarshalerStore marshalerStore, IMarshalingContext? parent, int relativeOffset, OffsetRelation offsetRelation, IMarshalingMetadata marshalingMetadata)
        {
            FieldName = fieldName;
            MarshalerStore = marshalerStore;
            Parent = parent;
            Metadata = marshalingMetadata;
            FieldAbsoluteOffset = FindRelation(offsetRelation).ItemAbsoluteOffset + relativeOffset;
        }

        public string FieldName { get; protected set; }
        public IMarshalerStore MarshalerStore { get; }
        public IMarshalingContext? Parent { get; }
        public IMarshalingMetadata Metadata { get; protected set; }
        public int FieldAbsoluteOffset { get; protected set; }
        public int ItemAbsoluteOffset => FieldAbsoluteOffset + (ItemOffset ?? 0);
        public int? ItemOffset { get; protected set; }

        public int? FieldLength { get; protected set; }
        public int? ItemLength { get; protected set; }
        public IMarshalingContext WithFieldByteLength(int? byteLength)
        {
            FieldLength = byteLength;
            return this;
        }
        public IMarshalingContext WithItemByteLength(int? itemByteLength)
        {
            ItemLength = itemByteLength;
            return this;
        }

        public void CorrectForCollectionItem(int itemOffset, int? itemLength)
        {
            ItemOffset = itemOffset;
            ItemLength = itemLength;
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
