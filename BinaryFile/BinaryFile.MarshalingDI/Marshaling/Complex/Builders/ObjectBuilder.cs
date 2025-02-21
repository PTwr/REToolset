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
using static BinaryFile.MarshalingDI.Context.HierarchicalFeatureSet;
using static BinaryFile.MarshalingDI.Context.IHierarchicalFeatureSet;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    //TODO private (file?) and return as interface?
    public partial class ObjectBuilder<TDeclaringType>
    {
        private MarshalingFeatures MarshalingFeatures = new MarshalingFeatures();
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
                    (pi, ctx) => pi.ParameterType == typeof(MarshalingFeatures),
                    (pi, ctx) => new MarshalingFeatures()
                    {
                        ReadFeatures = this.MarshalingFeatures.ReadFeatures
                            .Select(x => x.BoundCopy(ctx.Resolve<ILifetimeScope>()))
                            .ToList(),
                        WriteFeatures = this.MarshalingFeatures.WriteFeatures
                            .Select(x => x.BoundCopy(ctx.Resolve<ILifetimeScope>()))
                            .ToList(),
                    }))
                .InstancePerLifetimeScope();
        }

        //TODO move to extensions? keep base class pure of overloads? at leats move to partials?
        public ObjectBuilder<TDeclaringType>
            WithDebugInfo(Func<TDeclaringType, string> info)
        {
            var func = (ILifetimeScope c) => info(c
                .Resolve<IHierarchicalFeatureSet>()
                .GetRequired<TDeclaringType>(EMetadataNames.CurentObject.ToString()));
            var feature = new HierarchicalFeatureSet.FuncFeatureWrapper<string>(func, int.MaxValue, EMetadataNames.DebugInfo.ToString(), cached: true);
            WithReadMetadata(feature);
            WithWriteMetadata(feature);
            return this;
        }
        public ObjectBuilder<TDeclaringType>
            WithDebugInfo(string info)
        {
            var feature = new HierarchicalFeatureSet.ValueFeatureWrapper<string>(info, int.MaxValue, EMetadataNames.DebugInfo.ToString());
            WithReadMetadata(feature);
            WithWriteMetadata(feature);
            return this;
        }

        public ObjectBuilder<TDeclaringType>
            WithReadWriteMetadata(IFeatureWrapper feature)
        {
            WithReadMetadata(feature);
            WithWriteMetadata(feature);
            return this;
        }

        public ObjectBuilder<TDeclaringType>
            WithReadMetadata(IHierarchicalFeatureSet.IFeatureWrapper feature)
        {
            MarshalingFeatures.ReadFeatures.Add(feature);
            return this;
        }

        public ObjectBuilder<TDeclaringType>
            WithWriteMetadata(IHierarchicalFeatureSet.IFeatureWrapper feature)
        {
            MarshalingFeatures.WriteFeatures.Add(feature);
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
                .Resolve<IHierarchicalFeatureSet>()
                .GetRequired<TParent>(EMetadataNames.ParentObject.ToString()));
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
