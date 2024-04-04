using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using System.Diagnostics.CodeAnalysis;

namespace BinaryFile.Unpacker
{
    public interface ITypeMap
    {
        bool IsFor(Type type);
    }
    public abstract class SerializedAndDeserializerBase<TMappedType>
    {
        readonly protected Type MappedType = typeof(TMappedType);
        public virtual bool IsFor(Type type)
        {
            return type.IsAssignableTo(MappedType);
        }
    }

    public interface ISerializer : ITypeMap {  }
    public interface IDeserializer : ITypeMap { }
    public interface ISerializer<in TMappedType> : ISerializer
    {
        void Serialize(TMappedType value, ByteBuffer buffer, IMarshalingContext serializationContext, out int consumedLength);
    }
    public interface IDeserializer<out TMappedType> : IDeserializer
    {
        TMappedType Deserialize(Span<byte> data, IMarshalingContext deserializationContext, out int consumedLength);
    }

    public interface ISerializerManager
    {
        bool TryGetMapping<TType>([NotNullWhen(returnValue: true)] out ISerializer<TType>? serializer);
        void Register(ITypeMap deserializer);
    }
    public interface IDeserializerManager
    {
        bool TryGetMapping<TType>([NotNullWhen(returnValue: true)] out IDeserializer<TType>? deserializer);
        void Register(ITypeMap deserializer);
    }

    public class TypeMapStore<T>
        where T : ITypeMap
    {
        readonly List<T> maps = new List<T>();

        public void Register(T map)
        {
            //TODO maybe just hashset?
            if (!maps.Contains(map)) maps.Add(map);
        }

        public bool TryGetMapping<TType>([NotNullWhen(true)] out ITypeMap? map)
        {
            foreach (var m in maps)
            {
                if (m.IsFor(typeof(TType)))
                {
                    map = m;

                    return true;
                }
            }

            map = default;
            return false;
        }
    }

    public class MarshalerManager : 
        IDeserializerManager, ISerializerManager
    {
        TypeMapStore<ISerializer> Serializers = new TypeMapStore<ISerializer>();
        TypeMapStore<IDeserializer> Deserializers = new TypeMapStore<IDeserializer>();

        public void Register(ITypeMap map)
        {
            //TODO set rw flags on Into/From configs instead, to only accept configured maps
            if (map is IDeserializer) Deserializers.Register((IDeserializer)map);
            if (map is ISerializer) Serializers.Register((ISerializer)map);
        }

        //TODO ewww rewrite disgusting crap
        public bool TryGetMapping<TType>([NotNullWhen(true)] out ISerializer<TType>? serializer)
        {
            serializer = default;
            if (Serializers.TryGetMapping<TType>(out var s) && s is ISerializer<TType>)
            {
                serializer = (ISerializer<TType>)s;
                return true;
            }
            return false;
        }

        public bool TryGetMapping<TType>([NotNullWhen(true)] out IDeserializer<TType>? deserializer)
        {
            deserializer = default;
            if (Deserializers.TryGetMapping<TType>(out var s) && s is IDeserializer<TType>)
            {
                deserializer = (IDeserializer<TType>)s;
                return true;
            }
            return false;
        }
    }
}
