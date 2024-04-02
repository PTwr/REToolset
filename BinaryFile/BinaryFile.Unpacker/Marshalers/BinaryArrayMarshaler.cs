using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Marshalers
{
    public class BinaryArrayMarshaler :
        IDeserializer<bool[]>,
        IDeserializer<byte[]>,
        IDeserializer<sbyte[]>,
        IDeserializer<ushort[]>,
        IDeserializer<short[]>,
        IDeserializer<uint[]>,
        IDeserializer<int[]>,
        IDeserializer<ulong[]>,
        IDeserializer<long[]>,
        ISerializer<bool[]>,
        ISerializer<byte[]>,
        ISerializer<sbyte[]>,
        ISerializer<ushort[]>,
        ISerializer<short[]>,
        ISerializer<uint[]>,
        ISerializer<int[]>,
        ISerializer<ulong[]>,
        ISerializer<long[]>
    {
        public bool IsFor(Type type)
        {
            if (!type.IsArray) return false;

            type = type.GetElementType()!;

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
                //TODO (u)int24
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return false;
                default:
                    return false;
            }
        }

        private T[] Deserialize<T>(Span<byte> data, IMarshalingContext deIMarshalingContext, out int consumedLength)
            where T : struct
        {
            consumedLength = 0;

            data = deIMarshalingContext.Slice(data);

            var itemSize = Marshal.SizeOf<T>();
            int count = data.Length / itemSize;
            if (deIMarshalingContext.Count.HasValue)
            {
                if (count < deIMarshalingContext.Count.Value)
                    throw new ArgumentException($"{deIMarshalingContext.Name}. Requested count of {deIMarshalingContext.Count} exceedes data length of {data.Length} ({count} items max)");
                count = deIMarshalingContext.Count.Value;
            }
            T[] result = new T[count];

            int pos = 0;
            for (int i = 0; i < result.Length; i++, pos += itemSize)
            {
                var itemSlice =
                    itemSize > 1 ? //dont molest single bytes :)
                    data.Slice(pos, itemSize).NormalizeEndiannesInCopy(deIMarshalingContext.LittleEndian) :
                    data.Slice(pos, itemSize);
                result[i] = MemoryMarshal.Read<T>(itemSlice);
            }

            consumedLength = Marshal.SizeOf<T>() * count;
            return result;
        }

        bool[] IDeserializer<bool[]>.Deserialize(Span<byte> data, IMarshalingContext deIMarshalingContext, out int consumedLength)
        {
            var bytes = Deserialize<byte>(data, deIMarshalingContext, out consumedLength);
            //non-zero byte => true
            return bytes.Select(i => i > 0).ToArray();
        }

        byte[] IDeserializer<byte[]>.Deserialize(Span<byte> data, IMarshalingContext deIMarshalingContext, out int consumedLength)
        {
            return Deserialize<byte>(data, deIMarshalingContext, out consumedLength);
        }
        sbyte[] IDeserializer<sbyte[]>.Deserialize(Span<byte> data, IMarshalingContext deIMarshalingContext, out int consumedLength)
        {
            return Deserialize<sbyte>(data, deIMarshalingContext, out consumedLength);
        }
        ushort[] IDeserializer<ushort[]>.Deserialize(Span<byte> data, IMarshalingContext deIMarshalingContext, out int consumedLength)
        {
            return Deserialize<ushort>(data, deIMarshalingContext, out consumedLength);
        }
        short[] IDeserializer<short[]>.Deserialize(Span<byte> data, IMarshalingContext deIMarshalingContext, out int consumedLength)
        {
            return Deserialize<short>(data, deIMarshalingContext, out consumedLength);
        }
        uint[] IDeserializer<uint[]>.Deserialize(Span<byte> data, IMarshalingContext deIMarshalingContext, out int consumedLength)
        {
            return Deserialize<uint>(data, deIMarshalingContext, out consumedLength);
        }
        int[] IDeserializer<int[]>.Deserialize(Span<byte> data, IMarshalingContext deIMarshalingContext, out int consumedLength)
        {
            return Deserialize<int>(data, deIMarshalingContext, out consumedLength);
        }
        long[] IDeserializer<long[]>.Deserialize(Span<byte> data, IMarshalingContext deIMarshalingContext, out int consumedLength)
        {
            return Deserialize<long>(data, deIMarshalingContext, out consumedLength);
        }
        ulong[] IDeserializer<ulong[]>.Deserialize(Span<byte> data, IMarshalingContext deIMarshalingContext, out int consumedLength)
        {
            return Deserialize<ulong>(data, deIMarshalingContext, out consumedLength);
        }

        private void Serialize<T>(T[] values, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
            where T : struct
        {
            var itemSize = Marshal.SizeOf<T>();

            if (IMarshalingContext.Count.HasValue && values.Length != IMarshalingContext.Count)
            {
                throw new Exception($"{IMarshalingContext.Name}. Specified item count of {IMarshalingContext.Count} does not match actual count of {values.Length}");
            }

            consumedLength = itemSize * values.Length;
            int itemOffset = 0;
            for (int i = 0; i < values.Length; i++)
            {
                var itemSlice = buffer.Slice(IMarshalingContext.AbsoluteOffset + itemOffset, itemSize);

                if (itemSize > 1) itemSlice.NormalizeEndiannes(IMarshalingContext.LittleEndian);

                MemoryMarshal.Write(itemSlice, values[i]);

                itemOffset += itemSize;
            }
        }

        void ISerializer<bool[]>.Serialize(bool[] values, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            //should not need any manual bool->byte translation
            Serialize(values, buffer, IMarshalingContext, out consumedLength);
        }

        void ISerializer<byte[]>.Serialize(byte[] values, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(values, buffer, IMarshalingContext, out consumedLength);
        }
        void ISerializer<sbyte[]>.Serialize(sbyte[] values, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(values, buffer, IMarshalingContext, out consumedLength);
        }
        void ISerializer<ushort[]>.Serialize(ushort[] values, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(values, buffer, IMarshalingContext, out consumedLength);
        }
        void ISerializer<short[]>.Serialize(short[] values, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(values, buffer, IMarshalingContext, out consumedLength);
        }
        void ISerializer<uint[]>.Serialize(uint[] values, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(values, buffer, IMarshalingContext, out consumedLength);
        }
        void ISerializer<int[]>.Serialize(int[] values, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(values, buffer, IMarshalingContext, out consumedLength);
        }
        void ISerializer<long[]>.Serialize(long[] values, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(values, buffer, IMarshalingContext, out consumedLength);
        }
        void ISerializer<ulong[]>.Serialize(ulong[] values, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(values, buffer, IMarshalingContext, out consumedLength);
        }
    }
}
