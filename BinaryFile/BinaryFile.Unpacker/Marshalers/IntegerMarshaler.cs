using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BinaryFile.Unpacker.Marshalers
{
    //TODO cleanup
    public class IntegerMarshaler :
        IDeserializer<bool>,
        IDeserializer<byte>,
        IDeserializer<sbyte>,
        IDeserializer<ushort>,
        IDeserializer<short>,
        IDeserializer<UInt24>,
        IDeserializer<Int24>,
        IDeserializer<uint>,
        IDeserializer<int>,
        IDeserializer<ulong>,
        IDeserializer<long>,
        ISerializer<bool>,
        ISerializer<byte>,
        ISerializer<sbyte>,
        ISerializer<ushort>,
        ISerializer<short>,
        ISerializer<uint>,
        ISerializer<int>,
        ISerializer<UInt24>,
        ISerializer<Int24>,
        ISerializer<ulong>,
        ISerializer<long>
    {
        public bool IsFor(Type type)
        {
            if (type == typeof(UInt24) || type == typeof(Int24)) return true;

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

            if (data.Length < consumedLength) throw new Exception($"{marshalingContext.Name}. Data length of {data.Length} not enough to read {typeof(T).FullName} of size {consumedLength}");

            data = data.Slice(0, consumedLength);

            //dont waste effort reversing single bytes :)
            if (consumedLength > 1) data = data.NormalizeEndiannesInCopy(marshalingContext.LittleEndian);

            return MemoryMarshal.Read<T>(data);
        }

        bool IDeserializer<bool>.Deserialize(Span<byte> data, IMarshalingContext marshalingContext, out int consumedLength)
        {
            //non-zero byte => true
            return Deserialize<byte>(marshalingContext.Slice(data), marshalingContext, out consumedLength) > 0;
        }

        byte IDeserializer<byte>.Deserialize(Span<byte> data, IMarshalingContext marshalingContext, out int consumedLength)
        {
            return Deserialize<byte>(marshalingContext.Slice(data), marshalingContext, out consumedLength);
        }

        sbyte IDeserializer<sbyte>.Deserialize(Span<byte> data, IMarshalingContext marshalingContext, out int consumedLength)
        {
            return Deserialize<sbyte>(marshalingContext.Slice(data), marshalingContext, out consumedLength);
        }

        //TODO check if generic endiannes switcher works, and how badly its performance sucks
        //TODO check performance. native marshalingContext.LittleEndian is true ?  BinaryPrimitives.ReadUInt16LittleEndian(data) : BinaryPrimitives.ReadUInt16BigEndian(data) might be muuuuuch faster
        ushort IDeserializer<ushort>.Deserialize(Span<byte> data, IMarshalingContext marshalingContext, out int consumedLength)
        {
            return Deserialize<ushort>(marshalingContext.Slice(data), marshalingContext, out consumedLength);
        }

        short IDeserializer<short>.Deserialize(Span<byte> data, IMarshalingContext marshalingContext, out int consumedLength)
        {
            return Deserialize<short>(marshalingContext.Slice(data), marshalingContext, out consumedLength);
        }

        uint IDeserializer<uint>.Deserialize(Span<byte> data, IMarshalingContext marshalingContext, out int consumedLength)
        {
            return Deserialize<uint>(marshalingContext.Slice(data), marshalingContext, out consumedLength);
        }

        int IDeserializer<int>.Deserialize(Span<byte> data, IMarshalingContext marshalingContext, out int consumedLength)
        {
            return Deserialize<int>(marshalingContext.Slice(data), marshalingContext, out consumedLength);
        }

        UInt24 IDeserializer<UInt24>.Deserialize(Span<byte> data, IMarshalingContext marshalingContext, out int consumedLength)
        {
            data = marshalingContext.Slice(data);
            consumedLength = 3;

            if (data.Length < consumedLength) throw new Exception($"{marshalingContext.Name}. Data length of {data.Length} not enough to read {typeof(UInt24).FullName} of size {consumedLength}");

            data = data.Slice(0, consumedLength);

            //TODO uint24 does byte shits to combine bytes so this is unnecessary
            //data = data.NormalizeEndiannesInCopy(marshalingContext.LittleEndian);

            return new UInt24(data);
        }

        Int24 IDeserializer<Int24>.Deserialize(Span<byte> data, IMarshalingContext marshalingContext, out int consumedLength)
        {
            data = marshalingContext.Slice(data);
            consumedLength = 3;

            if (data.Length < consumedLength) throw new Exception($"{marshalingContext.Name}. Data length of {data.Length} not enough to read {typeof(UInt24).FullName} of size {consumedLength}");

            data = data.Slice(0, consumedLength);

            //TODO uint24 does byte shits to combine bytes so this is unnecessary
            //data = data.NormalizeEndiannesInCopy(marshalingContext.LittleEndian);

            return new Int24(data);
        }

        ulong IDeserializer<ulong>.Deserialize(Span<byte> data, IMarshalingContext marshalingContext, out int consumedLength)
        {
            return Deserialize<ulong>(marshalingContext.Slice(data), marshalingContext, out consumedLength);
        }

        long IDeserializer<long>.Deserialize(Span<byte> data, IMarshalingContext marshalingContext, out int consumedLength)
        {
            return Deserialize<long>(marshalingContext.Slice(data), marshalingContext, out consumedLength);
        }

        private void Serialize<T>(T value, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
            where T : struct
        {
            consumedLength = Marshal.SizeOf<T>();
            var slice = buffer.Slice(IMarshalingContext.AbsoluteOffset, consumedLength);
            MemoryMarshal.Write(slice, value);

            if (consumedLength > 1) slice.NormalizeEndiannes(IMarshalingContext.LittleEndian);
        }

        void ISerializer<bool>.Serialize(bool value, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            //non-zero byte => true
            Serialize(value ? (byte)1 : (byte)0, buffer, IMarshalingContext, out consumedLength);
        }

        void ISerializer<byte>.Serialize(byte value, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(value, buffer, IMarshalingContext, out consumedLength);
        }

        void ISerializer<sbyte>.Serialize(sbyte value, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(value, buffer, IMarshalingContext, out consumedLength);
        }

        //TODO check if generic endiannes switcher works, and how badly its performance sucks
        //TODO check performance. native IMarshalingContext.LittleEndian is true ?  BinaryPrimitives.ReadUInt16LittleEndian(data) : BinaryPrimitives.ReadUInt16BigEndian(data) might be muuuuuch faster
        void ISerializer<ushort>.Serialize(ushort value, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(value, buffer, IMarshalingContext, out consumedLength);
        }

        void ISerializer<short>.Serialize(short value, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(value, buffer, IMarshalingContext, out consumedLength);
        }

        void ISerializer<uint>.Serialize(uint value, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(value, buffer, IMarshalingContext, out consumedLength);
        }

        void ISerializer<int>.Serialize(int value, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(value, buffer, IMarshalingContext, out consumedLength);
        }

        void ISerializer<UInt24>.Serialize(UInt24 value, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            consumedLength = 3;
            var slice = buffer.Slice(IMarshalingContext.AbsoluteOffset, consumedLength);
            value.ToBytes().CopyTo(slice);

            //TODO uint24 already does this?
            //slice.NormalizeEndiannes(IMarshalingContext.LittleEndian);
        }

        void ISerializer<Int24>.Serialize(Int24 value, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            consumedLength = 3;
            var slice = buffer.Slice(IMarshalingContext.AbsoluteOffset, consumedLength);
            value.ToBytes().CopyTo(slice);

            //TODO uint24 already does this?
            //slice.NormalizeEndiannes(IMarshalingContext.LittleEndian);
        }

        void ISerializer<ulong>.Serialize(ulong value, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(value, buffer, IMarshalingContext, out consumedLength);
        }

        void ISerializer<long>.Serialize(long value, ByteBuffer buffer, IMarshalingContext IMarshalingContext, out int consumedLength)
        {
            Serialize(value, buffer, IMarshalingContext, out consumedLength);
        }
    }
}
