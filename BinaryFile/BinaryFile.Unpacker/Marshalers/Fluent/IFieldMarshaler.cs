using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Marshalers.Fluent
{
    public interface IFieldMarshaler<TDeclaringType>
    {
        void Deserialize(TDeclaringType declaringObject, Span<byte> bytes, IMarshalingContext deserializationContext, out int consumedLength);
        void Serialize(TDeclaringType declaringObject, ByteBuffer buffer, ISerializationContext serializationContext, out int consumedLength);
    }

    public interface IFluentSingularFieldMarshaler<TDeclaringType, TItem, TImplementation> :
        IFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentIntegerFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentStringFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentValidatedFieldDescriptor<TDeclaringType, TItem, TImplementation>
        where TImplementation : IFluentSingularFieldMarshaler<TDeclaringType, TItem, TImplementation>
    {
        TImplementation Into(Action<TDeclaringType, TItem> setter);
        TImplementation From(Func<TDeclaringType, TItem> getter);
    }

    public interface IFluentCollectionFieldMarshaler<TDeclaringType, TItem, TImplementation> :
        IFluentFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentIntegerFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentStringFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentCollectionFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentValidatedCollectionFieldDescriptor<TDeclaringType, TItem, TImplementation>
        where TImplementation : IFluentCollectionFieldMarshaler<TDeclaringType, TItem, TImplementation>
    {
        TImplementation Into(Action<TDeclaringType, IEnumerable<TItem>> setter);
        TImplementation From(Func<TDeclaringType, IEnumerable<TItem>> getter);
    }
}
