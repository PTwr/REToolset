﻿using BinaryDataHelper;
using BinaryFile.Unpacker.New.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Implementation.PrimitiveMarshalers
{
    public class IntegerMarshaler :
        IMarshaler<bool, bool>,
        IMarshaler<byte, byte>,
        IMarshaler<sbyte, sbyte>,
        IMarshaler<ushort, ushort>,
        IMarshaler<short, short>,
        IMarshaler<UInt24, UInt24>,
        IMarshaler<Int24, Int24>,
        IMarshaler<uint, uint>,
        IMarshaler<int, int>,
        IMarshaler<ulong, ulong>,
        IMarshaler<long, long>,
        IMarshaler<UInt128, UInt128>,
        IMarshaler<Int128, Int128>
    {
        private T Deserialize<T>(Span<byte> data, IMarshalingContext marshalingContext, out int consumedLength)
            where T : struct
        {
            consumedLength = Marshal.SizeOf<T>();

            if (data.Length < consumedLength) throw new Exception($"{marshalingContext.FieldName}. Data length of {data.Length} not enough to read {typeof(T).FullName} of size {consumedLength}");

            data = data.Slice(0, consumedLength);

            //dont waste effort reversing single bytes :)
            if (consumedLength > 1) data = data.NormalizeEndiannesInCopy(marshalingContext.Metadata.LittleEndian);

            return MemoryMarshal.Read<T>(data);
        }

        private void Serialize<T>(T value, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
            where T : struct
        {
            consumedLength = Marshal.SizeOf<T>();
            var slice = buffer.Slice(IMarshalingContext.ItemAbsoluteOffset, consumedLength);
            MemoryMarshal.Write(slice, value);

            if (consumedLength > 1) slice.NormalizeEndiannes(IMarshalingContext.Metadata.LittleEndian);
        }

        bool IDeserializingMarshaler<bool, bool>.DeserializeInto(bool mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<byte>(ctx.ItemSlice(data), ctx, out fieldByteLength) != 0;
        }

        void ISerializingMarshaler<bool>.SerializeFrom(bool mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject ? (byte)1 : (byte)0, data, ctx, out fieldByteLength);
        }

        byte IDeserializingMarshaler<byte, byte>.DeserializeInto(byte mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<byte>(ctx.ItemSlice(data), ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<byte>.SerializeFrom(byte mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        sbyte IDeserializingMarshaler<sbyte, sbyte>.DeserializeInto(sbyte mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<sbyte>(ctx.ItemSlice(data), ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<sbyte>.SerializeFrom(sbyte mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        ushort IDeserializingMarshaler<ushort, ushort>.DeserializeInto(ushort mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<ushort>(ctx.ItemSlice(data), ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<ushort>.SerializeFrom(ushort mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        short IDeserializingMarshaler<short, short>.DeserializeInto(short mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<short>(ctx.ItemSlice(data), ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<short>.SerializeFrom(short mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        UInt24 IDeserializingMarshaler<UInt24, UInt24>.DeserializeInto(UInt24 mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            data = ctx.ItemSlice(data);
            fieldByteLength = 3;

            if (data.Length < fieldByteLength) throw new Exception($"{ctx.FieldName}. Data length of {data.Length} not enough to read {typeof(UInt24).FullName} of size {fieldByteLength}");

            data = data.Slice(0, fieldByteLength);

            return new UInt24(data);
        }

        void ISerializingMarshaler<UInt24>.SerializeFrom(UInt24 mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            fieldByteLength = 3;
            var slice = data.Slice(ctx.ItemAbsoluteOffset, fieldByteLength);
            mappedObject.ToBytes().CopyTo(slice);
        }

        Int24 IDeserializingMarshaler<Int24, Int24>.DeserializeInto(Int24 mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<Int24>(ctx.ItemSlice(data), ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<Int24>.SerializeFrom(Int24 mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            fieldByteLength = 3;
            var slice = data.Slice(ctx.ItemAbsoluteOffset, fieldByteLength);
            mappedObject.ToBytes().CopyTo(slice);
        }

        uint IDeserializingMarshaler<uint, uint>.DeserializeInto(uint mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<uint>(ctx.ItemSlice(data), ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<uint>.SerializeFrom(uint mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        int IDeserializingMarshaler<int, int>.DeserializeInto(int mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<int>(ctx.ItemSlice(data), ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<int>.SerializeFrom(int mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        ulong IDeserializingMarshaler<ulong, ulong>.DeserializeInto(ulong mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<ulong>(ctx.ItemSlice(data), ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<ulong>.SerializeFrom(ulong mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        long IDeserializingMarshaler<long, long>.DeserializeInto(long mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<long>(ctx.ItemSlice(data), ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<long>.SerializeFrom(long mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        UInt128 IDeserializingMarshaler<UInt128, UInt128>.DeserializeInto(UInt128 mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<UInt128>(ctx.ItemSlice(data), ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<UInt128>.SerializeFrom(UInt128 mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        Int128 IDeserializingMarshaler<Int128, Int128>.DeserializeInto(Int128 mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<Int128>(ctx.ItemSlice(data), ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<Int128>.SerializeFrom(Int128 mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }
    }
}