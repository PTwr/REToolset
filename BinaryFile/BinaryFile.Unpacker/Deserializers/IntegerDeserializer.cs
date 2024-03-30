using BinaryFile.Unpacker.Metadata;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace BinaryFile.Unpacker.Deserializers
{
    //TODO indianness from ctx
    public class IntegerDeserializer :
        IDeserializer<bool>,
        IDeserializer<byte>,
        IDeserializer<sbyte>,
        IDeserializer<ushort>,
        IDeserializer<short>,
        IDeserializer<uint>,
        IDeserializer<int>,
        IDeserializer<ulong>,
        IDeserializer<long>
    {
        public bool IsFor(Type type)
        {
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

        private T Deserialize<T>(Span<byte> data, out bool success)
            where T : struct
        {
            success = MemoryMarshal.TryRead<T>(data, out var result);
            return result;
        }

        bool IDeserializer<bool>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            //non-zero byte => true
            return Deserialize<byte>(deserializationContext.Slice(data), out success) > 0;
        }

        byte IDeserializer<byte>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<byte>(deserializationContext.Slice(data), out success);
        }

        sbyte IDeserializer<sbyte>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<sbyte>(deserializationContext.Slice(data), out success);
        }

        ushort IDeserializer<ushort>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<ushort>(deserializationContext.Slice(data), out success);
        }

        short IDeserializer<short>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<short>(deserializationContext.Slice(data), out success);
        }

        uint IDeserializer<uint>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<uint>(deserializationContext.Slice(data), out success);
        }

        int IDeserializer<int>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<int>(deserializationContext.Slice(data), out success);
        }

        ulong IDeserializer<ulong>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<ulong>(deserializationContext.Slice(data), out success);
        }

        long IDeserializer<long>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext)
        {
            return Deserialize<long>(deserializationContext.Slice(data), out success);
        }
    }
}
