using BinaryDataHelper;
using BinaryFile.Unpacker.New.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Implementation
{
    public class IntegerMarshaler :
        //IMarshaler<bool>,
        IMarshaler<byte, byte>
    //IMarshaler<sbyte>,
    //IMarshaler<ushort>,
    //IMarshaler<short>,
    //IMarshaler<UInt24>,
    //IMarshaler<Int24>,
    //IMarshaler<uint>,
    //IMarshaler<int>,
    //IMarshaler<ulong>,
    //IMarshaler<long>
    {
        public byte DeserializeInto(byte mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            fieldByteLengh = 1;
            return data[ctx.ItemAbsoluteOffset];
        }

        public void SerializeFrom(byte mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            fieldByteLengh = 1;
            data.Emplace(ctx.ItemAbsoluteOffset, mappedObject);
        }
    }
}
