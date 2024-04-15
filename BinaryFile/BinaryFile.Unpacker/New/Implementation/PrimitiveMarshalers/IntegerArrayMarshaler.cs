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
        IMarshaler<long[], long[]>
    {

        private T[] Deserialize<T>(Span<byte> data, IMarshalingContext ctx, out int consumedLength)
            where T : struct
        {
            consumedLength = 0;

            data = ctx.ItemSlice(data);

            var itemSize = Marshal.SizeOf<T>();
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
                result[i] = MemoryMarshal.Read<T>(itemSlice);
            }

            consumedLength = Marshal.SizeOf<T>() * maxCount;
            return result;
        }

        private void Serialize<T>(T[] values, ByteBuffer buffer, IMarshalingContext ctx, out int consumedLength)
            where T : struct
        {
            var itemSize = Marshal.SizeOf<T>();

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

                MemoryMarshal.Write(itemSlice, values[i]);

                itemOffset += itemSize;
            }
        }

        bool[] IDeserializingMarshaler<bool[], bool[]>.DeserializeInto(bool[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            var bytes = Deserialize<byte>(data, ctx, out fieldByteLengh);
            //non-zero byte => true
            return bytes.Select(i => i > 0).ToArray();
        }
        void ISerializingMarshaler<bool[]>.SerializeFrom(bool[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            Serialize(value, data, ctx, out fieldByteLengh);
        }

        byte[] IDeserializingMarshaler<byte[], byte[]>.DeserializeInto(byte[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            return Deserialize<byte>(data, ctx, out fieldByteLengh);
        }
        void ISerializingMarshaler<byte[]>.SerializeFrom(byte[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            Serialize(value, data, ctx, out fieldByteLengh);
        }

        sbyte[] IDeserializingMarshaler<sbyte[], sbyte[]>.DeserializeInto(sbyte[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            return Deserialize<sbyte>(data, ctx, out fieldByteLengh);
        }
        void ISerializingMarshaler<sbyte[]>.SerializeFrom(sbyte[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            Serialize(value, data, ctx, out fieldByteLengh);
        }

        ushort[] IDeserializingMarshaler<ushort[], ushort[]>.DeserializeInto(ushort[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            return Deserialize<ushort>(data, ctx, out fieldByteLengh);
        }

        void ISerializingMarshaler<ushort[]>.SerializeFrom(ushort[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            Serialize(value, data, ctx, out fieldByteLengh);
        }

        short[] IDeserializingMarshaler<short[], short[]>.DeserializeInto(short[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            return Deserialize<short>(data, ctx, out fieldByteLengh);
        }

        void ISerializingMarshaler<short[]>.SerializeFrom(short[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            Serialize(value, data, ctx, out fieldByteLengh);
        }

        UInt24[] IDeserializingMarshaler<UInt24[], UInt24[]>.DeserializeInto(UInt24[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            return Deserialize<UInt24>(data, ctx, out fieldByteLengh);
        }

        void ISerializingMarshaler<UInt24[]>.SerializeFrom(UInt24[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            Serialize(value, data, ctx, out fieldByteLengh);
        }

        Int24[] IDeserializingMarshaler<Int24[], Int24[]>.DeserializeInto(Int24[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            return Deserialize<Int24>(data, ctx, out fieldByteLengh);
        }

        void ISerializingMarshaler<Int24[]>.SerializeFrom(Int24[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            Serialize(value, data, ctx, out fieldByteLengh);
        }

        uint[] IDeserializingMarshaler<uint[], uint[]>.DeserializeInto(uint[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            return Deserialize<uint>(data, ctx, out fieldByteLengh);
        }

        void ISerializingMarshaler<uint[]>.SerializeFrom(uint[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            Serialize(value, data, ctx, out fieldByteLengh);
        }

        int[] IDeserializingMarshaler<int[], int[]>.DeserializeInto(int[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            return Deserialize<int>(data, ctx, out fieldByteLengh);
        }

        void ISerializingMarshaler<int[]>.SerializeFrom(int[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            Serialize(value, data, ctx, out fieldByteLengh);
        }

        ulong[] IDeserializingMarshaler<ulong[], ulong[]>.DeserializeInto(ulong[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            return Deserialize<ulong>(data, ctx, out fieldByteLengh);
        }

        void ISerializingMarshaler<ulong[]>.SerializeFrom(ulong[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            Serialize(value, data, ctx, out fieldByteLengh);
        }

        long[] IDeserializingMarshaler<long[], long[]>.DeserializeInto(long[] value, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            return Deserialize<long>(data, ctx, out fieldByteLengh);
        }

        void ISerializingMarshaler<long[]>.SerializeFrom(long[] value, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            Serialize(value, data, ctx, out fieldByteLengh);
        }
    }
}
