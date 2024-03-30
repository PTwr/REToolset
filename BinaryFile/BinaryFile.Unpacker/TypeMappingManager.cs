using BinaryFile.Unpacker.Metadata;
using System.Diagnostics.CodeAnalysis;

namespace BinaryFile.Unpacker
{
    public abstract class SerializedAndDeserializerBase<TMappedType>
    {
        readonly protected Type MappedType = typeof(TMappedType);
        public virtual bool IsFor(Type type)
        {
            return type.IsAssignableTo(MappedType);
        }
    }


    public interface IDeserializer
    {
        bool IsFor(Type type);
    }
    public interface IDeserializer<out TMappedType> : IDeserializer
    {
        TMappedType Deserialize(Span<byte> data, out bool success, DeserializationContext deserializationContext, out int consumedLength);
    }

    public interface IDeserializerManager
    {
        bool TryGetMapping<TType>([NotNullWhen(returnValue: true)] out IDeserializer<TType>? deserializer);
        void Register(IDeserializer deserializer);
    }

    public class DeserializerManager : IDeserializerManager
    {
        readonly List<IDeserializer> deserializers = new List<IDeserializer>();

        public void Register(IDeserializer deserializer)
        {
            deserializers.Add(deserializer);
        }

        public bool TryGetMapping<TType>([NotNullWhen(true)] out IDeserializer<TType>? deserializer)
        {
            foreach(var m in deserializers)
            {
                if (m.IsFor(typeof(TType)))
                {
                    deserializer = m as IDeserializer<TType>;

                    return (deserializer is not null);
                }
            }

            deserializer = null;
            return false;
        }
    }
}
