using BinaryFile.Unpacker.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Interfaces
{
    public interface IOrderedFieldMarshaler<in TDeclaringType> : IMarshaler<TDeclaringType>
    {
        string Name { get; }
        int GetOrder(TDeclaringType mappedType);
        int GetSerializationOrder(TDeclaringType mappedType);
        int GetDeserializationOrder(TDeclaringType mappedType);

        bool IsDeserializationEnabled { get; }
        bool IsSerializationEnabled { get; }
    }
}
