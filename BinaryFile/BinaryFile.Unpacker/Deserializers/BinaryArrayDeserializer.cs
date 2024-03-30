using BinaryFile.Unpacker.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Deserializers
{
    //TODO indianness from ctx
    public class BinaryArrayDeserializer :
        IDeserializer<bool[]>,
        IDeserializer<byte[]>,
        IDeserializer<sbyte[]>,
        IDeserializer<ushort[]>,
        IDeserializer<short[]>,
        IDeserializer<uint[]>,
        IDeserializer<int[]>,
        IDeserializer<ulong[]>,
        IDeserializer<long[]>
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
                //TODO implement
                //TODO (u)int24
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return false;
                default:
                    return false;
            }
        }

        private T[] Deserialize<T>(Span<byte> data, out bool success, DeserializationContext deserializationContext, out int consumedLength)
            where T : struct
        {
            success = false;
            consumedLength = 0;

            data = deserializationContext.Slice(data);

            var itemSize = Marshal.SizeOf<T>();
            int count = data.Length / itemSize;
            if (deserializationContext.Count.HasValue)
            {
                if (count < deserializationContext.Count.Value) 
                    throw new ArgumentException($"Requested count of {deserializationContext.Count} exceedes data length of {data.Length} ({count} items max)");
                count = deserializationContext.Count.Value;
            }
            T[] result = new T[count];

            int pos = 0;
            for (int i = 0; i < result.Length; i++, pos += itemSize)
            {
                success = MemoryMarshal.TryRead<T>(data.Slice(pos), out var r);
                result[i] = r;
                if (!success) return default!;
            }

            consumedLength = Marshal.SizeOf<T>() * count;
            return result;
        }

        bool[] IDeserializer<bool[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext, out int consumedLength)
        {
            var bytes = Deserialize<byte>(data, out success, deserializationContext, out consumedLength);
            //non-zero byte => true
            return success ? bytes.Select(i => i > 0).ToArray() : default;
        }

        byte[] IDeserializer<byte[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<byte>(data, out success, deserializationContext, out consumedLength);
        }
        sbyte[] IDeserializer<sbyte[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<sbyte>(data, out success, deserializationContext, out consumedLength);
        }
        ushort[] IDeserializer<ushort[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<ushort>(data, out success, deserializationContext, out consumedLength);
        }
        short[] IDeserializer<short[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<short>(data, out success, deserializationContext, out consumedLength);
        }
        uint[] IDeserializer<uint[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<uint>(data, out success, deserializationContext, out consumedLength);
        }
        int[] IDeserializer<int[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<int>(data, out success, deserializationContext, out consumedLength);
        }
        long[] IDeserializer<long[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<long>(data, out success, deserializationContext, out consumedLength);
        }
        ulong[] IDeserializer<ulong[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<ulong>(data, out success, deserializationContext, out consumedLength);
        }
    }
}
