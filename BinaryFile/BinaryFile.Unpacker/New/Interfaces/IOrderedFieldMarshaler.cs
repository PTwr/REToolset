using BinaryFile.Unpacker.Metadata;
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
}
