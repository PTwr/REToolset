using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Marshalers
{
    //TODO add "native" support for string collections?
    public class StringMarshaler : IDeserializer<string>, ISerializer<string>
    {
        public string Deserialize(Span<byte> data, IMarshalingContext deserializationContext, out int consumedLength)
        {
            var bytes = deserializationContext.Slice(data);
            consumedLength = bytes.Length;

            if (deserializationContext.NullTerminated is true)
            {
                bytes = bytes.FindNullTerminator(out var noTerminator);
                consumedLength = noTerminator ? bytes.Length : bytes.Length + 1;
            }

            var encoding = deserializationContext.Encoding ?? Encoding.ASCII;

            var str = bytes.ToDecodedString(encoding);

            return str;
        }

        public void Serialize(string value, ByteBuffer buffer, ISerializationContext serializationContext, out int consumedLength)
        {
            var encoding = serializationContext.Encoding ?? Encoding.ASCII;
            var bytes = encoding.GetBytes(value);

            consumedLength = bytes.Length;
            buffer.Emplace(serializationContext.AbsoluteOffset, bytes.AsSpan());

            if (serializationContext.NullTerminated is true)
            {
                //TODO rewrite horrible and disgusting!
                buffer.Emplace(serializationContext.AbsoluteOffset + consumedLength, new byte[] { 0 }.AsSpan());

                consumedLength++;
            }
        }

        public bool IsFor(Type type)
        {
            return type == typeof(string);
        }
    }
}
