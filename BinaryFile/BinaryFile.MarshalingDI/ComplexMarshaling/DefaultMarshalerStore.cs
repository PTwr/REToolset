using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using ReflectionHelper;
using BinaryFile.MarshalingDI.Marshaling;
using Autofac;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Writing;

namespace BinaryFile.MarshalingDI.ComplexMarshaling
{
    public class DefaultMarshalerStore : IMarshalerStore
    {
        private readonly ILifetimeScope di;

        public DefaultMarshalerStore(ILifetimeScope di)
        {
            this.di = di;
        }

        private IEnumerable<TOut> DIEnumerate<TOut>(Type TExact, Func<TOut, bool> condition, EMarshalingType marshalingType)
            where TOut : IOrderedMarshaler
        {
            var marshalerType = typeof(TOut).GetGenericTypeDefinition().MakeGenericType(TExact);
            var marshalerCollectionType = typeof(IEnumerable<>).MakeGenericType(marshalerType);

            var marshalers = ((IEnumerable<IOrderedMarshaler>)di.Resolve(marshalerCollectionType))
                .Reverse()
                .OrderBy(x => x.Order(marshalingType));

            foreach (var marshalerCandidate in marshalers)
            {
                if (marshalerCandidate is TOut fieldMarshaler
                    &&
                    condition(fieldMarshaler))
                {
                    yield return fieldMarshaler;
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

        public bool TryGetReadMarshaler<TMarshaledType>(out IReadMarshaler<TMarshaledType> marshaler)
        {
            return TryGetReadMarshaler<TMarshaledType, TMarshaledType>(out marshaler);
        }
        public IReadMarshaler<TMarshaledType> GetReadMarshaler<TMarshaledType>()
        {
            return GetReadMarshaler<TMarshaledType, TMarshaledType>();
        }

        public bool TryGetReadMarshaler<TFieldType, TMarshaledType>(out IReadMarshaler<TFieldType> marshaler) where TMarshaledType : TFieldType
        {
            foreach (var type in typeof(TMarshaledType).EnumerateTypeHierarchy()
                .Concat(typeof(TMarshaledType).GetInterfaces()))
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
        public IReadMarshaler<TFieldType> GetReadMarshaler<TFieldType, TMarshaledType>()
            where TMarshaledType : TFieldType
        {
            if (TryGetReadMarshaler<TFieldType, TMarshaledType>(out var marshaler))
            {
                return marshaler;
            }

            throw new TypeLoadException($"Failed to locate ReadMarshaler for {typeof(TMarshaledType).FullName}");
        }

        public bool TryGetMutableReadMarshaler<TMarshaledType>(Type valueType, out IMutableReadMarshaler<TMarshaledType> marshaler)
        {
            foreach (var type in valueType.EnumerateTypeHierarchy()
                .Concat(valueType.GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach (var marshalerCandidate in DIEnumerate<IMutableReadMarshaler<TMarshaledType>>(type, (x) => x.IsForMutableReading(), EMarshalingType.Reading))
                {
                    //return first matching marshaler
                    marshaler = marshalerCandidate;
                    return true;
                }
            }

            marshaler = null!;
            return false;
        }
        public bool TryGetMutableReadMarshaler<TMarshaledType>(out IMutableReadMarshaler<TMarshaledType> marshaler)
        {
            return TryGetMutableReadMarshaler(typeof(TMarshaledType), out marshaler);
        }
        public IMutableReadMarshaler<TMarshaledType> GetMutableReadMarshaler<TMarshaledType>()
            where TMarshaledType : class
        {
            if (TryGetMutableReadMarshaler<TMarshaledType>(out var marshaler))
            {
                return marshaler;
            }
            throw new TypeLoadException($"Failed to locateMutable ReadMarshaler for {typeof(TMarshaledType).FullName}");
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
