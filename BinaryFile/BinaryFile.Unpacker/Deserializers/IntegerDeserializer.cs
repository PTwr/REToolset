using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace BinaryFile.Unpacker.Deserializers
{
    //TODO cleanup
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

        private T Deserialize<T>(Span<byte> data, DeserializationContext deserializationContext, out int consumedLength)
            where T : struct
        {
            consumedLength = Marshal.SizeOf<T>();

            if (data.Length < consumedLength) throw new Exception($"{deserializationContext.Name}. Data length of {data.Length} not enough to read {typeof(T).FullName} of size {consumedLength}");

            data = data.Slice(0, consumedLength);

            //dont waste effort reversing single bytes :)
            if (consumedLength > 1) data = data.NormalizeEndiannesInCopy(deserializationContext.LittleEndian);

            return MemoryMarshal.Read<T>(data);
        }

        bool IDeserializer<bool>.Deserialize(Span<byte> data, DeserializationContext deserializationContext, out int consumedLength)
        {
            //non-zero byte => true
            return Deserialize<byte>(deserializationContext.Slice(data), deserializationContext, out consumedLength) > 0;
        }

        byte IDeserializer<byte>.Deserialize(Span<byte> data, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<byte>(deserializationContext.Slice(data), deserializationContext, out consumedLength);
        }

        sbyte IDeserializer<sbyte>.Deserialize(Span<byte> data, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<sbyte>(deserializationContext.Slice(data), deserializationContext, out consumedLength);
        }

        //TODO check if generic endiannes switcher works, and how badly its performance sucks
        //TODO check performance. native deserializationContext.LittleEndian is true ?  BinaryPrimitives.ReadUInt16LittleEndian(data) : BinaryPrimitives.ReadUInt16BigEndian(data) might be muuuuuch faster
        ushort IDeserializer<ushort>.Deserialize(Span<byte> data, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<ushort>(deserializationContext.Slice(data), deserializationContext, out consumedLength);
        }

        short IDeserializer<short>.Deserialize(Span<byte> data, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<short>(deserializationContext.Slice(data), deserializationContext, out consumedLength);
        }

        uint IDeserializer<uint>.Deserialize(Span<byte> data, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<uint>(deserializationContext.Slice(data), deserializationContext, out consumedLength);
        }

        int IDeserializer<int>.Deserialize(Span<byte> data, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<int>(deserializationContext.Slice(data), deserializationContext, out consumedLength);
        }

        ulong IDeserializer<ulong>.Deserialize(Span<byte> data, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<ulong>(deserializationContext.Slice(data), deserializationContext, out consumedLength);
        }

        long IDeserializer<long>.Deserialize(Span<byte> data, DeserializationContext deserializationContext, out int consumedLength)
        {
            return Deserialize<long>(deserializationContext.Slice(data), deserializationContext, out consumedLength);
        }
    }
}
