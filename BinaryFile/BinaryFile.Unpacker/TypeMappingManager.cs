using System.Diagnostics.CodeAnalysis;

namespace BinaryFile.Unpacker
{
    public interface ITypeMappingManager
    {
        bool TryGetMapping<TType>([NotNullWhen(returnValue: true)] out ITypeMapping? typeMapping);
        void Register(ITypeMapping typeMapping);
    }

    public class TypeMappingManager : ITypeMappingManager
    {
        readonly List<ITypeMapping> typeMappings = new List<ITypeMapping>();

        public void Register(ITypeMapping typeMapping)
        {
            typeMappings.Add(typeMapping);
        }

        public bool TryGetMapping<TType>([NotNullWhen(true)] out ITypeMapping? typeMapping)
        {
            foreach(var m in typeMappings)
            {
                if (m.IsFor<TType>())
                {
                    typeMapping = m;
                    return true;
                }
            }

            typeMapping = null;
            return false;
        }
    }

    public interface ITypeMapping
    {
        bool IsFor(Type type);
        bool IsFor<TType>();
    }
    public interface ITypeMapping<TType> : ITypeMapping
    {
    }

    public abstract class TypeMapping<TMapedType> : ITypeMapping<TMapedType>
    {
        readonly Type MappedType;
        public TypeMapping()
        {
            MappedType = typeof(TMapedType);
        }

        public bool IsFor<TType>()
        {
            return IsFor(typeof(TType));
        }

        public bool IsFor(Type type)
        {
            return type.IsAssignableTo(MappedType);
        }
    }
}
