using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using ReflectionHelper;
using BinaryFile.MarshalingDI.Marshaling;
using Autofac;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Writing;
using System.Collections;

namespace BinaryFile.MarshalingDI.ComplexMarshaling
{
    public class CachedMarshalerStore : DefaultMarshalerStore
    {
        public CachedMarshalerStore(ILifetimeScope di) : base(di)
        {
        }

        private Dictionary<Type, IList> DIREsolutionCache = new Dictionary<Type, IList>();
        protected override IEnumerable<TOut> DIEnumerate<TOut>(Type TExact, EMarshalingType marshalingType)
        {
            if (DIREsolutionCache.TryGetValue(TExact, out var cached))
            {
                return cached
                    .OfType<TOut>();
            }

            //if everything is PerLifettimeScope, then it can be cached as Resolve is rather slow
            var result = base.DIEnumerate<TOut>(TExact, marshalingType).ToList();
            DIREsolutionCache[typeof(TOut)] = result;

            return result;
        }
    }

    public class DefaultMarshalerStore : IMarshalerStore
    {
        private readonly ILifetimeScope di;

        public DefaultMarshalerStore(ILifetimeScope di)
        {
            this.di = di;
        }

        protected virtual IEnumerable<TOut> DIEnumerate<TOut>(Type TExact, EMarshalingType marshalingType)
            where TOut : IOrderedMarshaler
        {
            var marshalerType = typeof(TOut).GetGenericTypeDefinition().MakeGenericType(TExact);
            var marshalerCollectionType = typeof(IEnumerable<>).MakeGenericType(marshalerType);

            var marshalers = ((IEnumerable<IOrderedMarshaler>)di.Resolve(marshalerCollectionType))
                .Reverse()
                .OrderBy(x => x.Order(marshalingType));

            foreach (var marshalerCandidate in marshalers)
            {
                if (marshalerCandidate is TOut fieldMarshaler)
                {
                    yield return fieldMarshaler;
                }
            }
        }

        protected virtual IEnumerable<TOut> DIEnumerate<TOut>(Type TExact, Func<TOut, bool> condition, EMarshalingType marshalingType)
            where TOut : IOrderedMarshaler
        {
            foreach (var marshalerCandidate in DIEnumerate<TOut>(TExact, marshalingType))
            {
                if (condition(marshalerCandidate))
                {
                    yield return marshalerCandidate;
                }
            }
        }

        public bool TryGetActivatorMarshaler<TMarshaledType>(out IActivatorMarshaler<TMarshaledType> marshaler)
        {
            foreach (var type in typeof(TMarshaledType).EnumerateTypeHierarchy()
                .Concat(typeof(TMarshaledType).GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach (var marshalerCandidate in DIEnumerate<IActivatorMarshaler<TMarshaledType>>(type, (x) => x.IsForActivating(), EMarshalingType.Activation))
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

        public bool TryGetReadMarshaler<TFieldType>(Type actualValueType, out IReadMarshaler<TFieldType> marshaler)
        {
            foreach (var type in actualValueType.EnumerateTypeHierarchy()
                .Concat(actualValueType.GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach (var marshalerCandidate in DIEnumerate<IReadMarshaler<TFieldType>>(type, (x) => x.IsForReading(), EMarshalingType.Reading))
                {
                    //return first matching marshaler
                    marshaler = marshalerCandidate;
                    return true;
                }
            }

            marshaler = null!;
            return false;
        }
        public IReadMarshaler<TFieldType> GetReadMarshaler<TFieldType>()
        {
            if (TryGetReadMarshaler<TFieldType>(out var marshaler))
            {
                return marshaler;
            }

            throw new TypeLoadException($"Failed to locate ReadMarshaler for {typeof(TFieldType).FullName}");
        }
        public IReadMarshaler<TFieldType> GetReadMarshaler<TFieldType>(Type actualValueType)
        {
            if (TryGetReadMarshaler<TFieldType>(actualValueType, out var marshaler))
            {
                return marshaler;
            }

            throw new TypeLoadException($"Failed to locate ReadMarshaler for {actualValueType.FullName}");
        }

        public bool TryGetWriteMarshaler<TMarshaledType>(TMarshaledType value, out IWriteMarshaler<TMarshaledType> marshaler)
        {
            foreach (var type in typeof(TMarshaledType).EnumerateTypeHierarchy()
                .Concat(typeof(TMarshaledType).GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach (var marshalerCandidate in DIEnumerate<IWriteMarshaler<TMarshaledType>>(type, (x) => x.IsForWriting(), EMarshalingType.Writing))
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
