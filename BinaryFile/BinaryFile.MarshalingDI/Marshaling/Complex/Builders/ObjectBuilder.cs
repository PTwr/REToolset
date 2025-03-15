using Autofac;
using Autofac.Core;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Complex.Builders;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;
using System;
using System.Xml.Linq;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    //TODO private (file?) and return as interface?
    public partial class ObjectBuilder<TDeclaringType>
    {
        private MarshalingFeaturesBuilder marshalingFeatures = new MarshalingFeaturesBuilder();
        private ObjectCallbacks<TDeclaringType> callbacks = new ObjectCallbacks<TDeclaringType>();

        public Guid Guid { get; } = Guid.NewGuid();
        public void RegisterInDI(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<ObjectMarshaler<TDeclaringType>>()
                .As<IActivatorMarshaler<TDeclaringType>>()
                .As<IMutableReadMarshaler<TDeclaringType>>()
                .As<IWriteMarshaler<TDeclaringType>>()
                .WithParameter(new ResolvedParameter(
                    (pi, ctx) => pi.ParameterType == typeof(ObjectCallbacks<TDeclaringType>),
                    (pi, ctx) => callbacks))
                .WithParameter(new ResolvedParameter(
                    (pi,ctx) => pi.ParameterType == typeof(IEnumerable<IFieldMarshaler<TDeclaringType>>),
                    (pi, ctx)=> ctx.ResolveKeyed<IEnumerable<IFieldMarshaler<TDeclaringType>>>(Guid)))
                .WithParameter(new ResolvedParameter(
                    (pi, ctx) => pi.ParameterType == typeof(MarshalingFeaturesBuilder),
                    (pi, ctx) => marshalingFeatures))
                .InstancePerLifetimeScope();
        }

        //TODO move to extensions? keep base class pure of overloads? at leats move to partials?
        public ObjectBuilder<TDeclaringType>
            WithDebugInfo(Func<TDeclaringType, string> info)
        {
            IFeature<string>.Func func = (IFeatureSet containingFeatureSet, ILifetimeScope diScope) =>
            {
                var currentObj = containingFeatureSet.GetCurentObject<TDeclaringType>();
                return info(currentObj);
            };

            WithReadWriteMetadata(func, false, EMetadataNames.DebugInfo.ToString(), int.MaxValue);
            return this;
        }
        public ObjectBuilder<TDeclaringType>
            WithDebugInfo(string info)
            => WithDebugInfo((x) => info);

        public ObjectBuilder<TDeclaringType>
            WithReadWriteMetadata<T>(IFeature<T>.Func func, bool cached, string? name = null, int maxAge = 0)
        {
            WithReadMetadata(func, cached, name, maxAge);
            WithWriteMetadata(func, cached, name, maxAge);
            return this;
        }

        public ObjectBuilder<TDeclaringType>
            WithReadMetadata<T>(IFeature<T>.Func func, bool cached, string? name = null, int maxAge = 0)
        {
            marshalingFeatures.AddReadFeature(func, cached, name, maxAge);
            return this;
        }

        public ObjectBuilder<TDeclaringType>
            WithWriteMetadata<T>(IFeature<T>.Func func, bool cached, string? name = null, int maxAge = 0)
        {
            marshalingFeatures.AddWriteFeature(func, cached, name, maxAge);
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
    }
}
