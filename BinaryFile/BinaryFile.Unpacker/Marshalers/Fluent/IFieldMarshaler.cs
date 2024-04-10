using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.Marshalers.Fluent
{
    public interface IFieldMarshalerBase<in TDeclaringType>
    {
        void Deserialize(TDeclaringType declaringObject, Span<byte> bytes, IMarshalingContext deserializationContext, out int consumedLength);
        void Serialize(TDeclaringType declaringObject, ByteBuffer buffer, IMarshalingContext serializationContext, out int consumedLength);
    }
    public interface IFieldMarshaler<TDeclaringType> : IFieldMarshalerBase<TDeclaringType>
    {
        string? Name { get; }

        bool DeserializationInitialized { get; }
        bool SerializationInitialized { get; }

        FuncField<TDeclaringType, int>? Order { get; }
        FuncField<TDeclaringType, int>? DeserializationOrder { get; }
        FuncField<TDeclaringType, int>? SerializationOrder { get; }
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
        IFluentValidatedCollectionFieldDescriptor<TDeclaringType, TItem, TImplementation>,
        IFluentFieldDescriptorEvents<TDeclaringType, TItem, TImplementation>
        where TImplementation : IFluentCollectionFieldMarshaler<TDeclaringType, TItem, TImplementation>
    {
        TImplementation Into(Action<TDeclaringType, IEnumerable<TItem>> setter);
        TImplementation From(Func<TDeclaringType, IEnumerable<TItem>> getter);
    }
}
