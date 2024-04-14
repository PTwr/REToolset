﻿using BinaryFile.Unpacker.New.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Implementation
{
    public class MarshalingContext : IMarshalingContext
    {
        private readonly string fieldName;

        public MarshalingContext(string fieldName, IMarshalerStore marshalerStore, IMarshalingContext? parent, int relativeOffset, Metadata.OffsetRelation offsetRelation)
        {
            this.fieldName = fieldName;
            MarshalerStore = marshalerStore;
            Parent = parent;

            FieldAbsoluteOffset = FindRelation(offsetRelation).FieldAbsoluteOffset + relativeOffset;
        }

        public IMarshalerStore MarshalerStore { get; }
        public IMarshalingContext? Parent { get; }

        public int FieldAbsoluteOffset { get; protected set; }
        public int ItemAbsoluteOffset => FieldAbsoluteOffset + (ItemOffset ?? 0);
        public int? ItemOffset { get; protected set; }

        public int? FieldLength { get; protected set; }
        public int? ItemLength { get; protected set; }

        public Span<byte> ItemSlice(Span<byte> source)
        {
            var slice = source.Slice(FieldAbsoluteOffset);
            if (FieldLength.HasValue)
            {
                if (FieldLength.Value > slice.Length)
                    throw new Exception($"Error when slicing for {fieldName}. Field Length of {FieldLength.Value} exceedes available length of {slice.Length}");
                slice = slice.Slice(0, FieldLength.Value);
            }

            if (ItemOffset.HasValue)
            {
                if (ItemOffset.Value > slice.Length)
                    throw new Exception($"Error when slicing for {fieldName}. Item Offset of {ItemOffset.Value} exceedes available length of {slice.Length}");
                slice = slice.Slice(ItemOffset.Value);
            }

            if (ItemLength.HasValue)
            {
                if (ItemLength.Value > slice.Length)
                    throw new Exception($"Error when slicing for {fieldName}. Item Length of {ItemLength.Value} exceedes available length of {slice.Length}");
                slice.Slice(0, ItemLength.Value);
            }

            return slice;
        }

        public virtual IMarshalingContext FindRelation(Metadata.OffsetRelation offsetRelation) =>
            offsetRelation == Metadata.OffsetRelation.Absolute ?
            Parent?.FindRelation(Metadata.OffsetRelation.Absolute) ?? this
            :
            offsetRelation == Metadata.OffsetRelation.Segment ? this : Parent?.FindRelation(offsetRelation - 1) ?? this;
    }
}