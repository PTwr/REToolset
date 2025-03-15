using Autofac.Core;
using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.Marshaling.Collection;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    public partial class CollectionFieldBuilder<TDeclaringType, TMarshaledType>
        : BaseFieldBuilder<TDeclaringType, TMarshaledType, CollectionFieldBuilder<TDeclaringType, TMarshaledType>, CollectionCallbacks<TDeclaringType, TMarshaledType>>
    {
        public CollectionFieldBuilder(ObjectBuilder<TDeclaringType> parent)
            : base(parent)
        {
        }

        public override ObjectBuilder<TDeclaringType>
            Done(ContainerBuilder containerBuilder)
        {
            containerBuilder
                .RegisterType<CollectionFieldMarshaler<TDeclaringType, TMarshaledType>>()
                .WithParameter(new ResolvedParameter(
                    (pi, ctx) => pi.ParameterType == typeof(CollectionCallbacks<TDeclaringType, TMarshaledType>),
                    (pi, ctx) => callbacks))
                .WithParameter(new ResolvedParameter(
                    (pi, ctx) => pi.ParameterType == typeof(MarshalingFeaturesBuilder),
                    (pi, ctx) => marshalingFeatures))
                .Keyed<IFieldMarshaler<TDeclaringType>>(parent.Guid)
                .InstancePerLifetimeScope();

            return parent;
        }

        //TODO cleanup raw methods and helpers
        public CollectionFieldBuilder<TDeclaringType, TMarshaledType>
            WithReadItemCountOf(Func<TDeclaringType, int> itemCount, bool cached = false)
        {
            IFeature<int>.Func func = (IFeatureSet containingFeatureSet, ILifetimeScope diScope) =>
            {
                var currentObj = containingFeatureSet.GetCurentObject<TDeclaringType>();
                return itemCount(currentObj);
            };

            this.WithReadMetadata(func, cached, EMetadataNames.CollectionCount.ToString(), 0);

            return this;
        }

        public CollectionFieldBuilder<TDeclaringType, TMarshaledType>
            WriteFrom(Func<TDeclaringType, IEnumerable<TMarshaledType>> getter)
        {
            callbacks.Getter = getter;
            return this;
        }

        public CollectionFieldBuilder<TDeclaringType, TMarshaledType>
            ReadInto(Action<TDeclaringType, (List<(int Offset, TMarshaledType? Value)> data, int bytesRead)> setter)
        {
            callbacks.Setter = setter!;
            return this;
        }
    }
}
