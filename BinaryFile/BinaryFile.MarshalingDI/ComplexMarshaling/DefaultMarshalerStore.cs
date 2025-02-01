using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using ReflectionHelper;
using BinaryFile.MarshalingDI.Marshaling;
using Autofac;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Writing;

namespace BinaryFile.MarshalingDI.ObjectMarshaling
{
    public class DefaultMarshalerStore : IMarshalerStore
    {
        private readonly IContainer container;

        public DefaultMarshalerStore(IContainer container)
        {
            this.container = container;
        }

        private IEnumerable<TOut> DIEnumerate<TOut>(Func<TOut, bool> condition)
            where TOut : IOrderedMarshaler
        {
            var marshalers = container.Resolve<IEnumerable<TOut>>()
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

            var marshalers = ((IEnumerable<IOrderedMarshaler>)container.Resolve(marshalerCollectionType))
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

        public IActivatorMarshaler<TMarshaledType> GetActivatorMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            foreach (var type in typeof(TMarshaledType).EnumerateTypeHierarchy()
                .Concat(typeof(TMarshaledType).GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach (var marshalerCandidate in DIEnumerate<IActivatorMarshaler<TMarshaledType>>(type, (x) => (x.IsForActivating(data, metadata, offsetStack, parent))))
                {
                    //return first matching marshaler
                    return marshalerCandidate;
                }
            }
            if (container.TryResolve<IActivatorMarshaler<TMarshaledType>>(out var exactMarshaler))
            {
                return exactMarshaler;
            }

            throw new TypeLoadException($"Failed to locate ActivatorMarshaler for {typeof(TMarshaledType).FullName}");
        }

        public IReadMarshaler<TFieldType> GetReadMarshaler<TFieldType, TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            where TMarshaledType : TFieldType
        {
            foreach (var type in typeof(TMarshaledType).EnumerateTypeHierarchy()
                .Concat(typeof(TMarshaledType).GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach(var marshalerCandidate in DIEnumerate<IReadMarshaler<TFieldType>>(type, (x) => x.IsForReading(data, metadata, offsetStack)))
                {
                    //return first matching marshaler
                    return marshalerCandidate;
                }
            }

            throw new TypeLoadException($"Failed to locate ReadMarshaler for {typeof(TMarshaledType).FullName}");
        }

        public IMutableReadMarshaler<TMarshaledType> GetMutableReadMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            where TMarshaledType : class
        {
            foreach (var type in typeof(TMarshaledType).EnumerateTypeHierarchy()
                .Concat(typeof(TMarshaledType).GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach (var marshalerCandidate in DIEnumerate<IMutableReadMarshaler<TMarshaledType>>(type, (x) => x.IsForMutableReading(data, metadata, offsetStack)))
                {
                    //return first matching marshaler
                    return marshalerCandidate;
                }
            }
            throw new TypeLoadException($"Failed to locate ReadMarshaler for {typeof(TMarshaledType).FullName}");
        }

        public IWriteMarshaler<TMarshaledType> GetWriteMarshaler<TMarshaledType>(TMarshaledType value)
        {
            foreach (var type in typeof(TMarshaledType).EnumerateTypeHierarchy()
                .Concat(typeof(TMarshaledType).GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach (var marshalerCandidate in DIEnumerate<IWriteMarshaler<TMarshaledType>>(type, (x) => x.IsForWriting(value)))
                {
                    //return first matching marshaler
                    return marshalerCandidate;
                }
            }

            throw new TypeLoadException($"Failed to locate WriteMarshaler for {typeof(TMarshaledType).FullName}");
        }
    }
}
