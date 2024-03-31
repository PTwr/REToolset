using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Deserializers
{
    //TODO add "native" support for string collections?
    public class StringDeserializer : IDeserializer<string>
    {
        string IDeserializer<string>.Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext, out int consumedLength)
        {
            var bytes = deserializationContext.Slice(data);
            consumedLength = bytes.Length;

            if (deserializationContext.NullTerminated is true)
            {
                bytes = bytes.FindNullTerminator(out var noTerminator);
                consumedLength = noTerminator ? bytes.Length : (bytes.Length + 1);
            }

            var encoding = deserializationContext.Encoding ?? Encoding.ASCII;

            var str = bytes.ToDecodedString(encoding);

            success = true;

            return str;
        }

        public bool IsFor(Type type)
        {
            return type == typeof(string);
        }
    }
}
