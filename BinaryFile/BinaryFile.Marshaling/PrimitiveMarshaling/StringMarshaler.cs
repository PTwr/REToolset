using BinaryDataHelper;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.PrimitiveMarshaling
{
    public class StringMarshaler
        : ITypeMarshaler<string>
    {
        public bool IsFor(Type t)
        {
            return t == typeof(string);
        }

        public string? Deserialize(string? str, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            var encoding = ctx.Metadata?.Encoding ?? Encoding.ASCII;

            var bytes = ctx.ItemSlice(data).Span;
            fieldByteLength = bytes.Length;

            if (ctx.Metadata?.NullTerminated is true)
            {
                bytes = bytes.FindNullTerminator(out var noTerminator, encoding: encoding);
                fieldByteLength = noTerminator ? bytes.Length : bytes.Length + 1;
            }

            str = bytes.ToDecodedString(encoding);

            return str;
        }

        public void Serialize(string? str, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            var encoding = ctx.Metadata?.Encoding ?? Encoding.ASCII;
            var b = str.ToBytes(encoding, ctx?.Metadata?.NullTerminated is true);

            fieldByteLength = b.Length;
            data.Emplace(ctx.ItemAbsoluteOffset, b);
        }
    }
}
