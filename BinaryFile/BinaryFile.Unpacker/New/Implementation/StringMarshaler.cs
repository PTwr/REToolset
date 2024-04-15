using BinaryDataHelper;
using BinaryFile.Unpacker.New.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Implementation
{
    public class StringMarshaler
        : IMarshaler<string, string>
    {
        public string DeserializeInto(string mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            var bytes = ctx.ItemSlice(data);
            fieldByteLengh = bytes.Length;

            if (ctx.Metadata.NullTerminated is true)
            {
                bytes = bytes.FindNullTerminator(out var noTerminator);
                fieldByteLengh = noTerminator ? bytes.Length : bytes.Length + 1;
            }

            var encoding = ctx.Metadata.Encoding ?? Encoding.ASCII;

            var str = bytes.ToDecodedString(encoding);

            return str;
        }

        public void SerializeFrom(string mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
        {
            var encoding = ctx.Metadata.Encoding ?? Encoding.ASCII;
            var bytes = encoding.GetBytes(mappedObject);

            fieldByteLengh = bytes.Length;
            data.Emplace(ctx.ItemAbsoluteOffset, bytes.AsSpan());

            if (ctx.Metadata.NullTerminated is true)
            {
                //TODO rewrite horrible and disgusting!
                data.Emplace(ctx.ItemAbsoluteOffset + fieldByteLengh, new byte[] { 0 }.AsSpan());

                fieldByteLengh++;
            }
        }
    }
}
