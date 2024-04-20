using BinaryDataHelper;
using BinaryFile.Unpacker.New.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Implementation.PrimitiveMarshalers
{
    public class IntegerArrayMarshaler :
        IMarshaler<bool[], bool[]>,
        IMarshaler<byte[], byte[]>,
        IMarshaler<sbyte[], sbyte[]>,
        IMarshaler<ushort[], ushort[]>,
        IMarshaler<short[], short[]>,
        IMarshaler<UInt24[], UInt24[]>,
        IMarshaler<Int24[], Int24[]>,
        IMarshaler<uint[], uint[]>,
        IMarshaler<int[], int[]>,
        IMarshaler<ulong[], ulong[]>,
        IMarshaler<long[], long[]>,
        IMarshaler<UInt128[], UInt128[]>,
        IMarshaler<Int128[], Int128[]>
    {
        delegate T Reader<T>(Span<byte> data);
        private T[] Deserialize<T>(Span<byte> data, IMarshalingContext ctx, int itemSize, Reader<T> marshaler, out int consumedLength)
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

        delegate void Writer<T>(Span<byte> data, T item);
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
                var itemSlice = buffer.Slice(ctx.ItemAbsoluteOffset + itemOffset, itemSize);

                if (itemSize > 1) itemSlice.NormalizeEndiannes(ctx.Metadata.LittleEndian);

                marshaler(itemSlice, values[i]);

                itemOffset += itemSize;
            }
        }

        private T[] Deserialize<T>(Span<byte> data, IMarshalingContext ctx, out int consumedLength)
            where T : struct
        {
            return Deserialize<T>(data, ctx, Marshal.SizeOf<T>(), d => MemoryMarshal.Read<T>(d), out consumedLength);
        }

        private void Serialize<T>(T[] values, ByteBuffer buffer, IMarshalingContext ctx, out int consumedLength)
            where T : struct
        {
            Serialize<T>(values, buffer, ctx, Marshal.SizeOf<T>(), (d, x) => MemoryMarshal.Write(d, x), out consumedLength);
        }

        bool[] IDeserializingMarshaler<bool[], bool[]>.DeserializeInto(bool[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            var bytes = Deserialize<byte>(data, ctx, out fieldByteLength);
            //non-zero byte => true
            return bytes.Select(i => i > 0).ToArray();
        }
        void ISerializingMarshaler<bool[]>.SerializeFrom(bool[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        byte[] IDeserializingMarshaler<byte[], byte[]>.DeserializeInto(byte[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<byte>(data, ctx, out fieldByteLength);
        }
        void ISerializingMarshaler<byte[]>.SerializeFrom(byte[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        sbyte[] IDeserializingMarshaler<sbyte[], sbyte[]>.DeserializeInto(sbyte[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<sbyte>(data, ctx, out fieldByteLength);
        }
        void ISerializingMarshaler<sbyte[]>.SerializeFrom(sbyte[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        ushort[] IDeserializingMarshaler<ushort[], ushort[]>.DeserializeInto(ushort[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<ushort>(data, ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<ushort[]>.SerializeFrom(ushort[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        short[] IDeserializingMarshaler<short[], short[]>.DeserializeInto(short[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<short>(data, ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<short[]>.SerializeFrom(short[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        UInt24[] IDeserializingMarshaler<UInt24[], UInt24[]>.DeserializeInto(UInt24[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<UInt24>(data, ctx, 24, i => new UInt24(i), out fieldByteLength);
        }

        void ISerializingMarshaler<UInt24[]>.SerializeFrom(UInt24[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, 24, (d, x) => x.Write(d), out fieldByteLength);
        }

        Int24[] IDeserializingMarshaler<Int24[], Int24[]>.DeserializeInto(Int24[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<Int24>(data, ctx, 24, i => new Int24(i), out fieldByteLength);
        }

        void ISerializingMarshaler<Int24[]>.SerializeFrom(Int24[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, 24, (d, x) => x.Write(d), out fieldByteLength);
        }

        uint[] IDeserializingMarshaler<uint[], uint[]>.DeserializeInto(uint[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<uint>(data, ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<uint[]>.SerializeFrom(uint[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        int[] IDeserializingMarshaler<int[], int[]>.DeserializeInto(int[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<int>(data, ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<int[]>.SerializeFrom(int[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        ulong[] IDeserializingMarshaler<ulong[], ulong[]>.DeserializeInto(ulong[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<ulong>(data, ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<ulong[]>.SerializeFrom(ulong[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        long[] IDeserializingMarshaler<long[], long[]>.DeserializeInto(long[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<long>(data, ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<long[]>.SerializeFrom(long[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        UInt128[] IDeserializingMarshaler<UInt128[], UInt128[]>.DeserializeInto(UInt128[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<UInt128>(data, ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<UInt128[]>.SerializeFrom(UInt128[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }

        Int128[] IDeserializingMarshaler<Int128[], Int128[]>.DeserializeInto(Int128[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            return Deserialize<Int128>(data, ctx, out fieldByteLength);
        }

        void ISerializingMarshaler<Int128[]>.SerializeFrom(Int128[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            Serialize(value, data, ctx, out fieldByteLength);
        }
    }
}
