using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using ReflectionHelper;
using BinaryFile.MarshalingDI.Marshaling;
using Autofac;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Writing;
using Microsoft.VisualBasic.FileIO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BinaryFile.MarshalingDI.ObjectMarshaling
{
    public class DefaultMarshalerStore : IMarshalerStore
    {
        private readonly ILifetimeScope di;

        public DefaultMarshalerStore(ILifetimeScope di)
        {
            this.di = di;
        }

        private IEnumerable<TOut> DIEnumerate<TOut>(Func<TOut, bool> condition)
            where TOut : IOrderedMarshaler
        {
            var marshalers = di.Resolve<IEnumerable<TOut>>()
                .OrderBy(x => x.Order);
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
        private IEnumerable<TOut> DIEnumerate<TOut>(Type TExact, Func<TOut, bool> condition)
            where TOut : IOrderedMarshaler
        {
            var marshalerType = typeof(TOut).GetGenericTypeDefinition().MakeGenericType(TExact);
            var marshalerCollectionType = typeof(IEnumerable<>).MakeGenericType(marshalerType);

            var marshalers = ((IEnumerable<IOrderedMarshaler>)di.Resolve(marshalerCollectionType))
                .Reverse()
                .OrderByDescending(x => x.Order);

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

        public bool TryGetActivatorMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent, out IActivatorMarshaler<TMarshaledType> marshaler)
        {
            foreach (var type in typeof(TMarshaledType).EnumerateTypeHierarchy()
                .Concat(typeof(TMarshaledType).GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach (var marshalerCandidate in DIEnumerate<IActivatorMarshaler<TMarshaledType>>(type, (x) => (x.IsForActivating(data, metadata, offsetStack, parent))))
                {
                    //return first matching marshaler
                    marshaler = marshalerCandidate;
                    return true;
                }
            }

            marshaler = null!;
            return false;
        }
        public IActivatorMarshaler<TMarshaledType> GetActivatorMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            if (TryGetActivatorMarshaler<TMarshaledType>(data, metadata, offsetStack, parent, out var marshaler))
            {
                return marshaler;
            }

            throw new TypeLoadException($"Failed to locate ActivatorMarshaler for {typeof(TMarshaledType).FullName}");
        }

        public bool TryGetReadMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out IReadMarshaler<TMarshaledType> marshaler)
        {
            return TryGetReadMarshaler<TMarshaledType, TMarshaledType>(data, metadata, offsetStack, out marshaler);
        }
        public IReadMarshaler<TMarshaledType> GetReadMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            return GetReadMarshaler<TMarshaledType, TMarshaledType>(data, metadata, offsetStack);
        }

        public bool TryGetReadMarshaler<TFieldType, TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out IReadMarshaler<TFieldType> marshaler) where TMarshaledType : TFieldType
        {
            foreach (var type in typeof(TMarshaledType).EnumerateTypeHierarchy()
                .Concat(typeof(TMarshaledType).GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach (var marshalerCandidate in DIEnumerate<IReadMarshaler<TFieldType>>(type, (x) => x.IsForReading(data, metadata, offsetStack)))
                {
                    //return first matching marshaler
                    marshaler = marshalerCandidate;
                    return true;
                }
            }

            marshaler = null!;
            return false;
        }
        public IReadMarshaler<TFieldType> GetReadMarshaler<TFieldType, TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            where TMarshaledType : TFieldType
        {
            if (TryGetReadMarshaler<TFieldType, TMarshaledType>(data, metadata, offsetStack, out var marshaler))
            {
                return marshaler;
            }

            throw new TypeLoadException($"Failed to locate ReadMarshaler for {typeof(TMarshaledType).FullName}");
        }

        public bool TryGetMutableReadMarshaler<TMarshaledType>(Type valueType, IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out IMutableReadMarshaler<TMarshaledType> marshaler)
        {
            foreach (var type in valueType.EnumerateTypeHierarchy()
                .Concat(valueType.GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach (var marshalerCandidate in DIEnumerate<IMutableReadMarshaler<TMarshaledType>>(type, (x) => x.IsForMutableReading(data, metadata, offsetStack)))
                {
                    //return first matching marshaler
                    marshaler = marshalerCandidate;
                    return true;
                }
            }

            marshaler = null!;
            return false;
        }
        public bool TryGetMutableReadMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out IMutableReadMarshaler<TMarshaledType> marshaler)
        {
            return TryGetMutableReadMarshaler<TMarshaledType>(typeof(TMarshaledType), data, metadata, offsetStack, out marshaler);
        }
        public IMutableReadMarshaler<TMarshaledType> GetMutableReadMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            where TMarshaledType : class
        {
            if (TryGetMutableReadMarshaler<TMarshaledType>(data, metadata, offsetStack, out var marshaler))
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
                foreach (var marshalerCandidate in DIEnumerate<IWriteMarshaler<TMarshaledType>>(type, (x) => x.IsForWriting(value)))
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
            if (TryGetWriteMarshaler<TMarshaledType>(value, out var marshaler))
            {
                return marshaler;
            }

            throw new TypeLoadException($"Failed to locate WriteMarshaler for {typeof(TMarshaledType).FullName}");
        }
    }
}
