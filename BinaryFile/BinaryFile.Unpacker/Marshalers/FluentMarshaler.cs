using BinaryDataHelper;
using BinaryFile.Unpacker.Marshalers.Fluent;
using BinaryFile.Unpacker.Metadata;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BinaryFile.Unpacker.Marshalers
{
    class A { }
    class B : A { }
    public class FluentMarshaler<TDeclaringType> : FluentMarshaler<TDeclaringType, TDeclaringType>
    {

    }
    public class FluentMarshaler<TDeclaringType, TBaseType> : SerializedAndDeserializerBase<TDeclaringType>, IDeserializer<TDeclaringType>, ISerializer<TDeclaringType>
        where TDeclaringType : TBaseType
    {
        List<IFieldMarshalerBase<TDeclaringType>> BaseClassFieldMarshalers = new List<IFieldMarshalerBase<TDeclaringType>>();
        List<IFieldMarshaler<TDeclaringType>> FieldMarshalers = new List<IFieldMarshaler<TDeclaringType>>();

        FluentMarshaler<TBaseType>? BaseTypeDescriptor = null;

        public FluentMarshaler<TDeclaringType, TBaseType> InheritsFrom(FluentMarshaler<TBaseType> baseTypeDescriptor)
        {
            BaseTypeDescriptor = baseTypeDescriptor;
            return this;
        }

        event OnBeforeDeserialization? onBeforeDeserialization = null;
        public delegate void OnBeforeDeserialization(Span<byte> data, IMarshalingContext ctx, TDeclaringType item);
        //TODO Event instead of Delegate? Or KISS and let helpers deal with multiple handlers?
        public FluentMarshaler<TDeclaringType, TBaseType> BeforeDeserialization(OnBeforeDeserialization eventHandler)
        {
            onBeforeDeserialization += eventHandler;
            return this;
        }

        void HandleBeforeDeserializationEvent(Span<byte> data, IMarshalingContext ctx, TDeclaringType item)
        {
            if (BaseTypeDescriptor is not null)
                BaseTypeDescriptor.HandleBeforeDeserializationEvent(data, ctx, item);

            if (onBeforeDeserialization is not null)
                onBeforeDeserialization(data, ctx, item);
        }

        public TDeclaringType Deserialize(Span<byte> data, IMarshalingContext deserializationContext, out int consumedLength)
        {
            //TODO length for type? For object fields its taken from field metadata, type marshaler could use length declaration
            consumedLength = 0;

            var declaringObject = deserializationContext.Activate<TDeclaringType>();

            if (declaringObject is null) throw new Exception($"Failed to create instance of {MappedType.FullName}!");

            HandleBeforeDeserializationEvent(data, deserializationContext, declaringObject);

            var oderedMarshalers = SortedDeserializers(declaringObject);
            foreach (var marshaler in oderedMarshalers)
            {
                marshaler.Deserialize(declaringObject, data, deserializationContext, out consumedLength);
            }

            return declaringObject;
        }

        private IEnumerable<IFieldMarshalerBase<TDeclaringType>> SortedDeserializers(TDeclaringType declaringObject)
        {
            var a = BaseTypeDescriptor?.SortedDeserializers(declaringObject)
                .Cast<IFieldMarshalerBase<TDeclaringType>>()
                .ToList();
            var b = FieldMarshalers
                .Where(i => i.DeserializationInitialized)
                .OrderBy(i => i.DeserializationOrder?.Get(declaringObject)
                ?? i.Order?.Get(declaringObject) ?? 0)
                .Cast<IFieldMarshalerBase<TDeclaringType>>()
                .ToList();

            if (a is null) return b;
            else return a.Concat(b);
        }

        public void Serialize(TDeclaringType declaringObject, ByteBuffer buffer, IMarshalingContext serializationContext, out int consumedLength)
        {
            consumedLength = 0;

            if (declaringObject is null) throw new ArgumentNullException($"Value is required. {MappedType.FullName}!");
            var oderedMarshalers = SortedSerializers(declaringObject);
            foreach (var marshaler in oderedMarshalers)
            {
                marshaler.Serialize(declaringObject, buffer, serializationContext, out consumedLength);
            }

        }

        private IEnumerable<IFieldMarshalerBase<TDeclaringType>> SortedSerializers(TDeclaringType declaringObject)
        {
            var a = BaseTypeDescriptor?.SortedSerializers(declaringObject)
                .Cast<IFieldMarshalerBase<TDeclaringType>>()
                .ToList();
            var b = FieldMarshalers
                .Where(i => i.SerializationInitialized)
                .OrderBy(i => i.SerializationOrder?.Get(declaringObject)
                ?? i.Order?.Get(declaringObject) ?? 0)
                .Cast<IFieldMarshalerBase<TDeclaringType>>()
                .ToList();

            if (a is null) return b;
            else return a.Concat(b);
        }

        //TODO switch to interfaces?
        public FluentUnaryFieldMarshaler<TDeclaringType, TItem> WithField<TItem>(string? name = null)
        {
            var descriptor = new FluentUnaryFieldMarshaler<TDeclaringType, TItem>(name);
            FieldMarshalers.Add(descriptor);
            return descriptor;
        }

        public FluentCollectionFieldMarshaler<TDeclaringType, TItem> WithCollectionOf<TItem>(string? name = null)
        {
            var descriptor = new FluentCollectionFieldMarshaler<TDeclaringType, TItem>(name);
            FieldMarshalers.Add(descriptor);
            return descriptor;
        }
    }
}
