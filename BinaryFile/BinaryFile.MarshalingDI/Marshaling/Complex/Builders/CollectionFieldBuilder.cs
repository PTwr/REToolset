using Autofac.Core;
using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.Marshaling.Collection;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers;
using static BinaryFile.MarshalingDI.Context.HierarchicalFeatureSet;

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
                .Keyed<IFieldMarshaler<TDeclaringType>>(parent.Guid)
                .InstancePerLifetimeScope();

            return parent;
        }

        //TODO cleanup raw methods and helpers
        public CollectionFieldBuilder<TDeclaringType, TMarshaledType>
            WithReadItemCountOf(Func<TDeclaringType, int> itemCount)
        {
            var func = (ILifetimeScope c) => itemCount(c.Resolve<IHierarchicalFeatureSet>().GetRequired<TDeclaringType>(EMetadataNames.ParentObject.ToString()));
            var feature = new FuncFeatureWrapper<int>(func, 1, EMetadataNames.CollectionCount.ToString());

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
