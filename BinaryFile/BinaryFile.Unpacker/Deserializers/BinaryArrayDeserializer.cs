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

        private T[] Deserialize<T>(Span<byte> data, out bool success)
            where T : struct
        {
            success = false;

            var itemSize = Marshal.SizeOf<T>();
            T[] result = new T[data.Length / itemSize];

            int pos = 0;
            for (int i = 0; i < result.Length; i++, pos += itemSize)
            {
                success = MemoryMarshal.TryRead<T>(data.Slice(pos), out var r);
                result[i] = r;
                if (!success) return default!;
            }

            return result;
        }

        bool[] IDeserializer<bool[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            var bytes = Deserialize<byte>(deserializationContext.Slice(data), out success);
            //non-zero byte => true
            return success ? bytes.Select(i => i > 0).ToArray() : default;
        }

        byte[] IDeserializer<byte[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<byte>(deserializationContext.Slice(data), out success);
        }
        sbyte[] IDeserializer<sbyte[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<sbyte>(deserializationContext.Slice(data), out success);
        }
        ushort[] IDeserializer<ushort[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<ushort>(deserializationContext.Slice(data), out success);
        }
        short[] IDeserializer<short[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<short>(deserializationContext.Slice(data), out success);
        }
        uint[] IDeserializer<uint[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<uint>(deserializationContext.Slice(data), out success);
        }
        int[] IDeserializer<int[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<int>(deserializationContext.Slice(data), out success);
        }
        long[] IDeserializer<long[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<long>(deserializationContext.Slice(data), out success);
        }
        ulong[] IDeserializer<ulong[]>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<ulong>(deserializationContext.Slice(data), out success);
        }
    }
}
