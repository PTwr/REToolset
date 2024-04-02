using BinaryFile.Unpacker.Deserializers.Fluent;
using BinaryFile.Unpacker.Metadata;
using Microsoft.VisualBasic.FileIO;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BinaryFile.Unpacker.Deserializers
{
    public class FluentDeserializer<TDeclaringType> : SerializedAndDeserializerBase<TDeclaringType>, IDeserializer<TDeclaringType>
    {
        List<IFluentFieldDescriptor<TDeclaringType>> Descriptors = new List<IFluentFieldDescriptor<TDeclaringType>>();

        public TDeclaringType Deserialize(Span<byte> data, DeserializationContext deserializationContext, out int consumedLength)
        {
            consumedLength = 0;
            //TODO safety checks
            //TODO improve
            //TODO ISegment<TParent>
            var declaringObject = (TDeclaringType)Activator.CreateInstance(MappedType);

            if (declaringObject is null) throw new Exception($"Failed to create instance of {MappedType.FullName}!");

            foreach (var descriptor in Descriptors)
            {
                descriptor.Deserialize(data, declaringObject, deserializationContext, out consumedLength);
            }

            return declaringObject;
        }

        public FluentFieldDescriptor<TDeclaringType, TItem> WithField<TItem>(string? name = null)
        {
            var descriptor = new FluentFieldDescriptor<TDeclaringType, TItem>(name);
            Descriptors.Add(descriptor);
            return descriptor;
        }

        public FluentCollectionDescriptor<TDeclaringType, TItem> WithCollectionOf<TItem>(string? name = null)
        {
            var descriptor = new FluentCollectionDescriptor<TDeclaringType, TItem>(name);
            Descriptors.Add(descriptor);
            return descriptor;
        }
    }
}
