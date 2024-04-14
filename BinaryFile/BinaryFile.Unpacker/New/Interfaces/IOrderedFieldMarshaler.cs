using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Interfaces
{
    public interface IOrderedFieldMarshaler<in TDeclaringType>
    {
        string Name { get; }
        int GetOrder(TDeclaringType mappedObject);
        int GetSerializationOrder(TDeclaringType mappedObject);
        int GetDeserializationOrder(TDeclaringType mappedObject);

        bool IsDeserializationEnabled { get; }
        bool IsSerializationEnabled { get; }

        void DeserializeInto(TDeclaringType mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh);
        void SerializeFrom(TDeclaringType mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh);
    }
}
