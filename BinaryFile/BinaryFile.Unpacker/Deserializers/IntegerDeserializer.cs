using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace BinaryFile.Unpacker.Deserializers
{
    public abstract class IntegerDeserializer<TIntType> : Deserializer<TIntType>
            where TIntType : struct
    {
        public override bool TryDeserialize(Span<byte> bytes, [NotNullWhen(true)] out TIntType result)
        {
            result = MemoryMarshal.Read<TIntType>(bytes);
            return true;
        }
    }

    public class ByteDeserializer : IntegerDeserializer<byte> { }
    public class SByteDeserializer : IntegerDeserializer<sbyte> { }

    //TODO indiannes from fieldinfo
    public class UInt16Deserializer : IntegerDeserializer<ushort> { }
    public class Int16Deserializer : IntegerDeserializer<short> { }

    //TODO indiannes from fieldinfo
    public class UInt32Deserializer : IntegerDeserializer<uint> { }
    public class Int32Deserializer : IntegerDeserializer<int> { }

    //TODO indiannes from fieldinfo
    public class UInt64Deserializer : IntegerDeserializer<ulong> { }
    public class Int64Deserializer : IntegerDeserializer<long> { }

    //TODO floats
    //TODO int24
}
