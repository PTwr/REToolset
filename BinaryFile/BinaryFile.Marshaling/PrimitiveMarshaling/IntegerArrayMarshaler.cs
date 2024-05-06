using BinaryDataHelper;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.TypeMarshaling;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.PrimitiveMarshaling
{
    public class IntegerArrayMarshaler :
        ITypeMarshaler<bool[]>,
        ITypeMarshaler<byte[]>,
        ITypeMarshaler<sbyte[]>,
        ITypeMarshaler<ushort[]>,
        ITypeMarshaler<short[]>,
        ITypeMarshaler<UInt24[]>,
        ITypeMarshaler<Int24[]>,
        ITypeMarshaler<uint[]>,
        ITypeMarshaler<int[]>,
        ITypeMarshaler<ulong[]>,
        ITypeMarshaler<long[]>,
        ITypeMarshaler<UInt128[]>,
        ITypeMarshaler<Int128[]>
    {
        public int Order => 0;

        delegate T Reader<T>(Memory<byte> data);
        private T[] Deserialize<T>(Memory<byte> data, IMarshalingContext ctx, int itemSize, Reader<T> marshaler, out int consumedLength)
        {
            consumedLength = 0;

            data = ctx.ItemSlice(data);

            int maxCount = data.Length / itemSize;
            int? requestedCount = ctx.Metadata.ItemCount;
            if (requestedCount.HasValue)
            {
                if (maxCount < requestedCount.Value)
                    throw new ArgumentException($"{ctx.FieldName}. Requested count of {requestedCount} exceedes data length of {data.Length} ({maxCount} items max)");
                maxCount = requestedCount.Value;
            }
            T[] result = new T[maxCount];

            int pos = 0;
            for (int i = 0; i < result.Length; i++, pos += itemSize)
            {
                var itemSlice =
                    itemSize > 1 ? //dont molest single bytes :)
                    data.Slice(pos, itemSize).NormalizeEndiannesInCopy(ctx.Metadata.LittleEndian) :
                    data.Slice(pos, itemSize);
                result[i] = marshaler(itemSlice);
            }

            consumedLength = itemSize * maxCount;
            return result;
        }

        delegate void Writer<T>(Memory<byte> data, T item);
        private void Serialize<T>(T[] values, ByteBuffer buffer, IMarshalingContext ctx, int itemSize, Writer<T> marshaler, out int consumedLength)
        {
            int? requestedCount = ctx.Metadata.ItemCount;
            if (requestedCount.HasValue && values.Length != requestedCount)
            {
                throw new Exception($"{ctx.FieldName}. Specified item count of {requestedCount} does not match actual count of {values.Length}");
            }

            consumedLength = itemSize * values.Length;
            int itemOffset = 0;
            for (int i = 0; i < values.Length; i++)
            {
                var itemSlice = buffer.SliceMemory(ctx.ItemAbsoluteOffset + itemOffset, itemSize);

                if (itemSize > 1) itemSlice.NormalizeEndiannes(ctx.Metadata.LittleEndian);

                marshaler(itemSlice, values[i]);

                itemOffset += itemSize;
            }
        }

        private T[] Deserialize<T>(Memory<byte> data, IMarshalingContext ctx, out int consumedLength)
            where T : struct
        {
            return Deserialize(data, ctx, Marshal.SizeOf<T>(), d => MemoryMarshal.Read<T>(d.Span), out consumedLength);
        }

        private void Serialize<T>(T[]? values, ByteBuffer buffer, IMarshalingContext ctx, out int consumedLength)
            where T : struct
        {
            consumedLength = 0;
            if (values is null) return;
            Serialize(values, buffer, ctx, Marshal.SizeOf<T>(), (d, x) => MemoryMarshal.Write(d.Span, x), out consumedLength);
        }

        bool[] ITypeMarshaler<bool[]>.Deserialize(bool[]? value, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            var bytes = Deserialize<byte>(data, ctx, out fieldByteLength);
            //non-zero byte => true
            return bytes.Select(i => i > 0).ToArray();
        }
        void ITypeMarshaler<bool[]>.Serialize(bool[]? value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        byte[] ITypeMarshaler<byte[]>.Deserialize(byte[]? value, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<byte>(data, ctx, out fieldByteLength);
        }
        void ITypeMarshaler<byte[]>.Serialize(byte[]? value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        sbyte[] ITypeMarshaler<sbyte[]>.Deserialize(sbyte[]? value, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<sbyte>(data, ctx, out fieldByteLength);
        }
        void ITypeMarshaler<sbyte[]>.Serialize(sbyte[]? value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        ushort[] ITypeMarshaler<ushort[]>.Deserialize(ushort[]? value, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<ushort>(data, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<ushort[]>.Serialize(ushort[]? value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        short[] ITypeMarshaler<short[]>.Deserialize(short[]? value, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<short>(data, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<short[]>.Serialize(short[]? value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        UInt24[] ITypeMarshaler<UInt24[]>.Deserialize(UInt24[]? value, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize(data, ctx, 24, i => new UInt24(i.Span), out fieldByteLength);
        }

        void ITypeMarshaler<UInt24[]>.Serialize(UInt24[]? value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, 24, (d, x) => x.Write(d.Span), out fieldByteLength);
        }

        Int24[] ITypeMarshaler<Int24[]>.Deserialize(Int24[]? value, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize(data, ctx, 24, i => new Int24(i.Span), out fieldByteLength);
        }

        void ITypeMarshaler<Int24[]>.Serialize(Int24[]? value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, 24, (d, x) => x.Write(d.Span), out fieldByteLength);
        }

        uint[] ITypeMarshaler<uint[]>.Deserialize(uint[]? value, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<uint>(data, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<uint[]>.Serialize(uint[]? value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        int[] ITypeMarshaler<int[]>.Deserialize(int[]? value, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<int>(data, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<int[]>.Serialize(int[]? value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        ulong[] ITypeMarshaler<ulong[]>.Deserialize(ulong[]? value, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<ulong>(data, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<ulong[]>.Serialize(ulong[]? value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        long[] ITypeMarshaler<long[]>.Deserialize(long[]? value, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<long>(data, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<long[]>.Serialize(long[]? value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        UInt128[] ITypeMarshaler<UInt128[]>.Deserialize(UInt128[]? value, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<UInt128>(data, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<UInt128[]>.Serialize(UInt128[]? value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        Int128[] ITypeMarshaler<Int128[]>.Deserialize(Int128[]? value, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<Int128>(data, ctx, out fieldByteLength);
        }

        void ITypeMarshaler<Int128[]>.Serialize(Int128[]? value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        public bool IsFor(Type t)
        {
            if (t.IsArray is false) return false;

            var type = t.GetElementType();

            //TODO unify with IntegerMarshaler type check
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
    }
}
