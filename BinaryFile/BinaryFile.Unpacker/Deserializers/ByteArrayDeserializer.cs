using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Deserializers
{
    public class ByteArrayDeserializer : Deserializer<byte[]>
    {
        public override bool TryDeserialize(Span<byte> bytes, [NotNullWhen(true)] out byte[] result)
        {
            result = bytes.ToArray();
            return true;
        }
    }
}
