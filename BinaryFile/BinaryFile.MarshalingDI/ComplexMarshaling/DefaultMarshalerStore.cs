using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using ReflectionHelper;
using BinaryFile.MarshalingDI.Marshaling;
using Autofac;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Writing;
using System.Collections;
using Microsoft.VisualBasic.FileIO;
using LanguageExt;

namespace BinaryFile.MarshalingDI.ComplexMarshaling
{
    public class CachedMarshalerStore : DefaultMarshalerStore
    {
        public CachedMarshalerStore(ILifetimeScope di) : base(di)
        {
        }

        private readonly Dictionary<(Type requestedType, Type inheritedType), IList> _marshalerTypeCache = [];
        protected override IEnumerable<TOut> EnumerateFromDI<TOut>(Type TExact, EMarshalingType marshalingType)
        {
            //TOut implies MarshalingType by being specific interface of activation mode
            if (!_marshalerTypeCache.TryGetValue((typeof(TOut), TExact), out var list))
            {
                _marshalerTypeCache[(typeof(TOut), TExact)] = list = base.EnumerateFromDI<TOut>(TExact, marshalingType).ToList();

            }
            return (List<TOut>)list;
        }

        private Dictionary<Type, List<Type>> _typeHierarchyCache = [];
        protected override IEnumerable<Type> EnumerateTypeHierarchyWithInterfaces(Type TMarshaledType)
        {
            if (!_typeHierarchyCache.TryGetValue(TMarshaledType, out var list))
            {
                if (TMarshaledType == typeof(String))
                {
                    list = [TMarshaledType];
                }
                //TODO ugh, gotta read https://stackoverflow.com/questions/34670901/in-c-when-does-type-fullname-return-null
                else if (TMarshaledType.FullName.StartsWith("System.") && TMarshaledType.GetInterfaces().Any(x => x.FullName.StartsWith("System.Numerics.INumber")))
                {
                    list = [TMarshaledType];
                }
                else
                {
                    list = base.EnumerateTypeHierarchyWithInterfaces(TMarshaledType)
                        //TODO filter out more framework crap .NET Core added to primitive types :D
                        .Except([typeof(ValueType), typeof(Object)])
                        .ToList();
                }
                _typeHierarchyCache[TMarshaledType] = list;
            }
            return list;
        }
    }

    public class DefaultMarshalerStore : IMarshalerStore
    {
        private readonly ILifetimeScope di;

        public DefaultMarshalerStore(ILifetimeScope di)
        {
            this.di = di;
        }

        protected virtual IEnumerable<TOut> EnumerateFromDI<TOut>(Type TExact, EMarshalingType marshalingType)
        {
            var marshalerType = typeof(TOut).GetGenericTypeDefinition().MakeGenericType(TExact);
            var marshalerCollectionType = typeof(IEnumerable<>).MakeGenericType(marshalerType);

            var marshalers = ((IEnumerable<IOrderedMarshaler>)di.Resolve(marshalerCollectionType))
                .Reverse()
                .OrderBy(x => x.Order(marshalingType));

            return marshalers.OfType<TOut>();
        }
        protected virtual TOut? FindInDI<TOut>(Type TExact, Func<TOut, bool> condition, EMarshalingType marshalingType)
            where TOut : IOrderedMarshaler
        {
            var candidates = EnumerateFromDI<TOut>(TExact, marshalingType);

            foreach (var marshalerCandidate in candidates)
            {
                if (marshalerCandidate is TOut fieldMarshaler && condition(fieldMarshaler))
                {
                    return fieldMarshaler;
                }
            }

            return default;
        }

        protected virtual IEnumerable<Type> EnumerateTypeHierarchyWithInterfaces<TMarshaledType>()
        {
            return EnumerateTypeHierarchyWithInterfaces(typeof(TMarshaledType));
        }
        protected virtual IEnumerable<Type> EnumerateTypeHierarchyWithInterfaces(Type TMarshaledType)
        {
            return TMarshaledType.EnumerateTypeHierarchy()
                            .Concat(TMarshaledType.GetInterfaces());
        }

        public virtual bool TryGetActivatorMarshaler<TMarshaledType>(out IActivatorMarshaler<TMarshaledType> marshaler)
        {
            foreach (var type in EnumerateTypeHierarchyWithInterfaces<TMarshaledType>())
            {
                //starting from exact type and crawling down, then through interfaces
                var marshalerCandidate = FindInDI<IActivatorMarshaler<TMarshaledType>>(type, (x) => x.IsForActivating(), EMarshalingType.Activation);
                if (marshalerCandidate is not null)
                {
                    //return first matching marshaler
                    marshaler = marshalerCandidate;
                    return true;
                }
            }

            marshaler = null!;
            return false;
        }

        //todo parent through features
        public IActivatorMarshaler<TMarshaledType> GetActivatorMarshaler<TMarshaledType>()
        {
            if (TryGetActivatorMarshaler<TMarshaledType>(out var marshaler))
            {
                return marshaler;
            }

            throw new TypeLoadException($"Failed to locate ActivatorMarshaler for {typeof(TMarshaledType).FullName}");
        }

        public bool TryGetReadMarshaler<TFieldType>(out IReadMarshaler<TFieldType> marshaler)
        {
            return TryGetReadMarshaler(typeof(TFieldType), out marshaler);
        }

        public virtual bool TryGetReadMarshaler<TMarshaledType>(Type actualValueType, out IReadMarshaler<TMarshaledType> marshaler)
        {
            foreach (var type in EnumerateTypeHierarchyWithInterfaces(actualValueType))
            {
                //starting from exact type and crawling down, then through interfaces
                var marshalerCandidate = FindInDI<IReadMarshaler<TMarshaledType>>(type, (x) => x.IsForReading(), EMarshalingType.Reading);
                if (marshalerCandidate is not null)
                {
                    //return first matching marshaler
                    marshaler = marshalerCandidate;
                    return true;
                }
            }

            marshaler = null!;
            return false;
        }
        public IReadMarshaler<TMarshaledType> GetReadMarshaler<TMarshaledType>()
        {
            if (TryGetReadMarshaler<TMarshaledType>(out var marshaler))
            {
                return marshaler;
            }

            throw new TypeLoadException($"Failed to locate ReadMarshaler for {typeof(TMarshaledType).FullName}");
        }
        public IReadMarshaler<TMarshaledType> GetReadMarshaler<TMarshaledType>(Type actualValueType)
        {
            if (TryGetReadMarshaler<TMarshaledType>(actualValueType, out var marshaler))
            {
                return marshaler;
            }

            throw new TypeLoadException($"Failed to locate ReadMarshaler for {actualValueType.FullName}");
        }

        public virtual bool TryGetWriteMarshaler<TMarshaledType>(TMarshaledType value, out IWriteMarshaler<TMarshaledType> marshaler)
        {
            foreach (var type in EnumerateTypeHierarchyWithInterfaces<TMarshaledType>())
            {
                //starting from exact type and crawling down, then through interfaces
                var marshalerCandidate = FindInDI<IWriteMarshaler<TMarshaledType>>(type, (x) => x.IsForWriting(), EMarshalingType.Writing);
                if (marshalerCandidate is not null)
                {
                    //return first matching marshaler
                    marshaler = marshalerCandidate;
                    return true;
                }
            }

            marshaler = null!;
            return false;
        }
        public IWriteMarshaler<TMarshaledType> GetWriteMarshaler<TMarshaledType>(TMarshaledType value)
        {
            if (TryGetWriteMarshaler(value, out var marshaler))
            {
                return marshaler;
            }

            throw new TypeLoadException($"Failed to locate WriteMarshaler for {typeof(TMarshaledType).FullName}");
        }
    }
}
