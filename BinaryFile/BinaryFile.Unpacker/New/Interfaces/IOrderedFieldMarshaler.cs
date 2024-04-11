using BinaryDataHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Interfaces
{
    public interface IOrderedFieldMarshaler<in TMappedType> : IMarshaler<TMappedType>
    {
        string Name { get; }
        int GetOrder(TMappedType mappedType);
        int GetSerializationOrder(TMappedType mappedType);
        int GetDeserializationOrder(TMappedType mappedType);

        bool IsDeserializationEnabled { get; }
        bool IsSerializationEnabled { get; }
    }

    public interface IMarshaler<in TMappedType>
    {
        void Deserialize(TMappedType mappedObject, IFluentMarshalingContext ctx, Span<byte> data, out int fieldByteLengh);
        void Serialize(TMappedType mappedObject, IFluentMarshalingContext ctx, ByteBuffer data, out int fieldByteLengh);
    }
}
