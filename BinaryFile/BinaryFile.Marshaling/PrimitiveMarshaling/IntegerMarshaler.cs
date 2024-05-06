using BinaryDataHelper;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.PrimitiveMarshaling
{
    public class IntegerMarshaler :
        ITypeMarshaler<bool>,
        ITypeMarshaler<byte>,
        ITypeMarshaler<sbyte>,
        ITypeMarshaler<ushort>,
        ITypeMarshaler<short>,
        ITypeMarshaler<UInt24>,
        ITypeMarshaler<Int24>,
        ITypeMarshaler<uint>,
        ITypeMarshaler<int>,
        ITypeMarshaler<ulong>,
        ITypeMarshaler<long>,
        ITypeMarshaler<UInt128>,
        ITypeMarshaler<Int128>
    {
        public int Order => 0;

        public bool IsFor(Type type)
        {
            if (type == typeof(UInt24) || type == typeof(Int24)) return true;
            if (type == typeof(UInt128) || type == typeof(Int128)) return true;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.Int16:
                case TypeCode.UInt32:
                case TypeCode.Int32:
                case TypeCode.UInt64:
                case TypeCode.Int64:
                    return true;
                //TODO implement floats
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return false;
                default:
                    return false;
            }
        }

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

        bool ITypeMarshaler<bool>.Deserialize(bool obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<byte>(ctx.ItemSlice(data).Span, ctx, out fieldByteLength) != 0;
        }

        void ITypeMarshaler<bool>.Serialize(bool mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject ? (byte)1 : (byte)0, data, ctx, out fieldByteLength);
        }

        byte ITypeMarshaler<byte>.Deserialize(byte obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<byte>(ctx.ItemSlice(data).Span, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<byte>.Serialize(byte mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        sbyte ITypeMarshaler<sbyte>.Deserialize(sbyte obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<sbyte>(ctx.ItemSlice(data).Span, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<sbyte>.Serialize(sbyte mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        ushort ITypeMarshaler<ushort>.Deserialize(ushort obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<ushort>(ctx.ItemSlice(data).Span, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<ushort>.Serialize(ushort mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        short ITypeMarshaler<short>.Deserialize(short obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<short>(ctx.ItemSlice(data).Span, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<short>.Serialize(short mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        UInt24 ITypeMarshaler<UInt24>.Deserialize(UInt24 obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            var span = ctx.ItemSlice(data).Span;
            fieldByteLength = 3;

            if (span.Length < fieldByteLength) throw new Exception($"{ctx.FieldName}. Data length of {data.Length} not enough to read {typeof(UInt24).FullName} of size {fieldByteLength}");

            span = span.Slice(0, fieldByteLength);

            return new UInt24(span);
        }

        void ITypeMarshaler<UInt24>.Serialize(UInt24 mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            fieldByteLength = 3;
            var slice = data.Slice(ctx.ItemAbsoluteOffset, fieldByteLength);
            mappedObject.ToBytes().CopyTo(slice);
        }

        Int24 ITypeMarshaler<Int24>.Deserialize(Int24 obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            var span = ctx.ItemSlice(data).Span;
            fieldByteLength = 3;

            if (span.Length < fieldByteLength) throw new Exception($"{ctx.FieldName}. Data length of {data.Length} not enough to read {typeof(Int24).FullName} of size {fieldByteLength}");

            span = span.Slice(0, fieldByteLength);

            return new Int24(span);
        }

        void ITypeMarshaler<Int24>.Serialize(Int24 mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            fieldByteLength = 3;
            var slice = data.Slice(ctx.ItemAbsoluteOffset, fieldByteLength);
            mappedObject.ToBytes().CopyTo(slice);
        }

        uint ITypeMarshaler<uint>.Deserialize(uint obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<uint>(ctx.ItemSlice(data).Span, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<uint>.Serialize(uint mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        int ITypeMarshaler<int>.Deserialize(int obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<int>(ctx.ItemSlice(data).Span, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<int>.Serialize(int mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        ulong ITypeMarshaler<ulong>.Deserialize(ulong obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<ulong>(ctx.ItemSlice(data).Span, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<ulong>.Serialize(ulong mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        long ITypeMarshaler<long>.Deserialize(long obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<long>(ctx.ItemSlice(data).Span, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<long>.Serialize(long mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        UInt128 ITypeMarshaler<UInt128>.Deserialize(UInt128 obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<UInt128>(ctx.ItemSlice(data).Span, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<UInt128>.Serialize(UInt128 mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }

        Int128 ITypeMarshaler<Int128>.Deserialize(Int128 obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<Int128>(ctx.ItemSlice(data).Span, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<Int128>.Serialize(Int128 mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(mappedObject, data, ctx, out fieldByteLength);
        }
    }
}
