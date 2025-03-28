using Autofac;
using Autofac.Core;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.DI;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Complex.Builders;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;
using BinaryDataHelper;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    public partial class ObjectBuilder<TDeclaringType>
        : BaseBuilder<TDeclaringType, ObjectBuilder<TDeclaringType>, ObjectCallbacks<TDeclaringType>>
    {
        protected List<Type> additionalTypes = [];

        public Guid Guid { get; } = Guid.NewGuid();
        public void RegisterInDI(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<ObjectMarshaler<TDeclaringType>>()
                .As<IActivatorMarshaler<TDeclaringType>>()
                .As<IReadMarshaler<TDeclaringType>>()
                .As<IWriteMarshaler<TDeclaringType>>()
                .As(additionalTypes.ToArray())
                .WithParameter(new ResolvedParameter(
                    (pi, ctx) => pi.ParameterType == typeof(ObjectCallbacks<TDeclaringType>),
                    (pi, ctx) => callbacks))
                .WithParameter(new ResolvedParameter(
                    (pi,ctx) => pi.ParameterType == typeof(IEnumerable<IFieldMarshaler<TDeclaringType>>),
                    (pi, ctx)=> ctx.ResolveKeyed<IEnumerable<IFieldMarshaler<TDeclaringType>>>(Guid))) //bind fieldmarshalers to ID of ObjectMarshalerBuilder
                .WithParameter(new ResolvedParameter(
                    (pi, ctx) => pi.ParameterType == typeof(MarshalingFeaturesBuilder),
                    (pi, ctx) => marshalingFeatures))
                .InstancePerLifetimeScope();
        }

        public ObjectBuilder<TDeclaringType>
            WithMagicPatternOf(byte?[] pattern)
        {
            callbacks.IsForActivating = (di) =>
                di.Resolve<IDataBuffer>().AsSpan(di.Resolve<IOffsetStack>().CurrentAbsoluteOffset).StartsWith(pattern);
            callbacks.IsForReading = (di) =>
                di.Resolve<IDataBuffer>().AsSpan(di.Resolve<IOffsetStack>().CurrentAbsoluteOffset).StartsWith(pattern);
            return this;
        }
        public ObjectBuilder<TDeclaringType>
            AlsoActivateFor<T>()
        {
            additionalTypes.Add(typeof(IActivatorMarshaler<T>));
            //additionalTypes.Add(typeof(IMutableReadMarshaler<T>));
            return this;
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
            WithDefaultActivator(Func<ILifetimeScope, TDeclaringType?> activator)
        {
            callbacks.DefaultActivator = activator;
            return this;
        }
        public ObjectBuilder<TDeclaringType>
            WithDefaultActivator<TParent>(Func<TParent, TDeclaringType?> activator)
        {
            var func = (ILifetimeScope c) => activator(c
                .Resolve<IFeatureSetStack>()
                .GetRequiredValue<TParent>(EMetadataNames.ParentObject.ToString()));
            callbacks.DefaultActivator = func;
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
            var builder = new CollectionFieldBuilder<TDeclaringType, TMarshaledType>(this);
            return builder;
        }

        public ObjectBuilder<TDeclaringType>
            WithActivationOrderOf(Func<int> order)
        {
            callbacks.ActivationOrder = order;
            return this;
        }
        public ObjectBuilder<TDeclaringType>
            WithReadingOrderOf(Func<int> order)
        {
            callbacks.ReadingOrder = order;
            return this;
        }
        public ObjectBuilder<TDeclaringType>
            WithWritingOrderOf(Func<int> order)
        {
            callbacks.WritingOrder = order;
            return this;
        }
    }
}
