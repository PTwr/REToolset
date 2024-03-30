using BinaryFile.Unpacker.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Deserializers
{
    public class FluentDeserializer<TDeclaringType> : IDeserializer<TDeclaringType>
    {
        public TDeclaringType Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext, out int consumedLength)
        {
            throw new NotImplementedException();
        }

        public bool IsFor(Type type)
        {
            throw new NotImplementedException();
        }
    }
}
