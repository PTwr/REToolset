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
    public interface IDeserializer<TMappedType> : IDeserializer
    {
        bool TryDeserialize(Span<byte> bytes, [NotNullWhen(returnValue: true)] out TMappedType result);
    }
    public abstract class Deserializer<TMappedType> : IDeserializer<TMappedType>
    {
        readonly protected Type MappedType = typeof(TMappedType);
        public abstract bool TryDeserialize(Span<byte> bytes, [NotNullWhen(returnValue: true)] out TMappedType result);

        public virtual bool IsFor(Type type)
        {
            return type.IsAssignableTo(MappedType);
        }
    }

    public interface IDeserializerManager
    {
        bool TryGetMapping<TType>([NotNullWhen(returnValue: true)] out IDeserializer? typeMapping);
        void Register(IDeserializer typeMapping);
    }

    public class DeserializerManager : IDeserializerManager
    {
        readonly List<IDeserializer> deserializers = new List<IDeserializer>();

        public void Register(IDeserializer typeMapping)
        {
            deserializers.Add(typeMapping);
        }

        public bool TryGetMapping<TType>([NotNullWhen(true)] out IDeserializer? typeMapping)
        {
            foreach(var m in deserializers)
            {
                if (m.IsFor(typeof(TType)))
                {
                    typeMapping = m;
                    return true;
                }
            }

            typeMapping = null;
            return false;
        }
    }
}
