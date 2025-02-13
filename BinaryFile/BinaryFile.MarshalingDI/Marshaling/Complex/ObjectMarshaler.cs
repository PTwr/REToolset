using Autofac;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Complex.Builders;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    public partial class ObjectMarshaler<TDeclaringType> : IFullMutableMarshaler<TDeclaringType>
    {
        private readonly IMarshalerStore marshalerStore;
        private readonly Callbacks callbacks;

        private ObjectMarshaler(IMarshalerStore marshalerStore, Callbacks callbacks)
        {
            this.marshalerStore = marshalerStore;
            this.callbacks = callbacks;
        }

        private partial record Callbacks
        {
            public List<Func<IMarshalerStore, IFieldMarshaler<TDeclaringType>>> FieldMarshalerInitalizers = new List<Func<IMarshalerStore, IFieldMarshaler<TDeclaringType>>>();
            public Func<int> ActivationOrder = () => 0;
            public Func<int> ReadingOrder = () => 0;
            public Func<int> WritingOrder = () => 0;
            public Func<TDeclaringType, int> BytesRead = (x) => 0;
            public Func<TDeclaringType, int> BytesWrote = (x) => 0;
            public Func<object?, TDeclaringType?> DefaultActivator = (x) => default;
        }

        //TODO private (file?) and return as interface?
        public partial class Builder
        {
            private Callbacks callbacks = new Callbacks();

            public void RegisterInDI(ContainerBuilder containerBuilder)
            {
                containerBuilder.Register((ctx) =>
                    {
                        var store = ctx.Resolve<IMarshalerStore>();
                        var objMarshaler = new ObjectMarshaler<TDeclaringType>(store, callbacks);

                        return objMarshaler;
                    })
                    .As<IActivatorMarshaler<TDeclaringType>>()
                    .As<IMutableReadMarshaler<TDeclaringType>>()
                    .As<IWriteMarshaler<TDeclaringType>>();
            }

            public Builder WithReadByteLengthOf(Func<TDeclaringType, int> byteLength)
            {
                callbacks.BytesRead = byteLength;
                return this;
            }

            public Builder WithWriteByteLengthOf(Func<TDeclaringType, int> byteLength)
            {
                callbacks.BytesWrote = byteLength;
                return this;
            }

            /// <summary>
            /// Activator executed if all conditional activators pass through without activation
            /// </summary>
            /// <param name="activator"></param>
            /// <returns></returns>
            public Builder WithDefaultActivator(Func<object?, TDeclaringType?> activator)
            {
                callbacks.DefaultActivator = activator;
                return this;
            }

            public Builder RegisterFieldMarshalerInitializer(Func<IMarshalerStore, IFieldMarshaler<TDeclaringType>> initializer)
            {
                callbacks.FieldMarshalerInitalizers.Add(initializer);
                return this;
            }

            public UnaryFieldBuilder<TDeclaringType, TMarshaledType> WithFieldOf<TMarshaledType>()
            {
                var builder = new UnaryFieldBuilder<TDeclaringType, TMarshaledType>(this);
                return builder;
            }
            public void WithCollectionOf<TMarshaledType>()
            {
                //TODO fallback to normal marshaling if marshaler for specific collection type is registered? Huge optimization for stuff like byte[]
                //TODO unifying unary field and collections would suck and pollute fluent with unnecessary config methods, keep separate?
                throw new NotImplementedException();
            }
        }

        public TDeclaringType? Activate(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            return callbacks.DefaultActivator(parent);
        }

        public void Read(TDeclaringType value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            //TODO cache?
            foreach (var fieldMarshaler in callbacks.FieldMarshalerInitalizers
                .Select(x => x(marshalerStore))
                .Where(x => x.IsForReading(value))
                .OrderBy(x => x.ReadOrder(value))
                )
            {
                fieldMarshaler.ReadField(value, data, out _, metadata, offsetStack);
            }

            bytesRead = callbacks.BytesRead(value);
        }

        public void Write(TDeclaringType value, IDataBuffer data, out int bytesWrote, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            //TODO cache?
            foreach (var fieldMarshaler in callbacks.FieldMarshalerInitalizers
                .Select(x => x(marshalerStore))
                .Where(x => x.IsForWriting(value))
                .OrderBy(x => x.WriteOrder(value))
                )
            {
                //TODO return objectByteSize (required for collection item offsets
                fieldMarshaler.WriteField(value, data, out _, metadata, offsetStack);
            }
            bytesWrote = callbacks.BytesWrote(value);
        }

        public int Order(EMarshalingType marshalingType)
        {
            switch (marshalingType)
            {
                case EMarshalingType.Activation:
                    return callbacks.ActivationOrder();
                case EMarshalingType.Reading:
                    return callbacks.ReadingOrder();
                case EMarshalingType.Writing:
                    return callbacks.WritingOrder();
                default:
                    throw new ArgumentException($"Unkown {nameof(EMarshalingType)} value of {marshalingType}!");
            }
        }

    }
}
