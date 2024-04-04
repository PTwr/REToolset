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
    public class FluentMarshaler<TDeclaringType> : SerializedAndDeserializerBase<TDeclaringType>, IDeserializer<TDeclaringType>, ISerializer<TDeclaringType>
    {
        List<IFieldMarshaler<TDeclaringType>> FieldMarshalers = new List<IFieldMarshaler<TDeclaringType>>();

        public TDeclaringType Deserialize(Span<byte> data, IMarshalingContext deserializationContext, out int consumedLength)
        {
            //TODO length for type? For object fields its taken from field metadata, type marshaler could use length declaration
            consumedLength = 0;

            var declaringObject = deserializationContext.Activate<TDeclaringType>();

            if (declaringObject is null) throw new Exception($"Failed to create instance of {MappedType.FullName}!");

            foreach (var marshaler in FieldMarshalers)
            {
                marshaler.Deserialize(declaringObject, data, deserializationContext, out consumedLength);
            }

            return declaringObject;
        }

        public void Serialize(TDeclaringType declaringObject, ByteBuffer buffer, IMarshalingContext serializationContext, out int consumedLength)
        {
            consumedLength = 0;

            if (declaringObject is null) throw new ArgumentNullException($"Value is required. {MappedType.FullName}!");

            foreach (var marshaler in FieldMarshalers)
            {
                marshaler.Serialize(declaringObject, buffer, serializationContext, out consumedLength);
            }

        }

        //TODO switch to interfaces?
        public FluentSingularFieldMarshaler<TDeclaringType, TItem> WithField<TItem>(string? name = null)
        {
            var descriptor = new FluentSingularFieldMarshaler<TDeclaringType, TItem>(name);
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
