using Autofac;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Complex.Builders;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    //TODO private (file?) and return as interface?
    public partial class ObjectBuilder<TDeclaringType>
    {
        private ObjectCallbacks<TDeclaringType> callbacks = new ObjectCallbacks<TDeclaringType>();

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

        public ObjectBuilder<TDeclaringType>
            WithReadByteLengthOf(Func<TDeclaringType, int> byteLength)
        {
            callbacks.BytesRead = byteLength;
            return this;
        }

        public ObjectBuilder<TDeclaringType>
            WithWriteByteLengthOf(Func<TDeclaringType, int> byteLength)
        {
            callbacks.BytesWrote = byteLength;
            return this;
        }

        /// <summary>
        /// Activator executed if all conditional activators pass through without activation
        /// </summary>
        /// <param name="activator"></param>
        /// <returns></returns>
        public ObjectBuilder<TDeclaringType>
            WithDefaultActivator(Func<object?, TDeclaringType?> activator)
        {
            callbacks.DefaultActivator = activator;
            return this;
        }

        public ObjectBuilder<TDeclaringType>
            RegisterFieldMarshalerInitializer(Func<IMarshalerStore, IFieldMarshaler<TDeclaringType>> initializer)
        {
            callbacks.FieldMarshalerInitalizers.Add(initializer);
            return this;
        }

        public UnaryFieldBuilder<TDeclaringType, TMarshaledType> 
            WithFieldOf<TMarshaledType>()
        {
            var builder = new UnaryFieldBuilder<TDeclaringType, TMarshaledType>(this);
            return builder;
        }
        public CollectionFieldBuilder<TDeclaringType, TMarshaledType>
            WithCollectionOf<TMarshaledType>()
        {
            //TODO fallback to normal marshaling if marshaler for specific collection type is registered? Huge optimization for stuff like byte[]
            //TODO unifying unary field and collections would suck and pollute fluent with unnecessary config methods, keep separate?
            throw new NotImplementedException();
        }
    }
}
