using Autofac;
using Autofac.Core;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers;
using BinaryFile.MarshalingDI.Marshaling.Helpers;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    public partial class UnaryFieldBuilder<TDeclaringType, TMarshaledType>
        : BaseFieldBuilder<TDeclaringType, TMarshaledType, UnaryFieldBuilder<TDeclaringType, TMarshaledType>, UnaryCallbacks<TDeclaringType, TMarshaledType>>
    {
        protected internal UnaryFieldBuilder(ObjectBuilder<TDeclaringType> parent)
            : base(parent)
        {
        }

        public override ObjectBuilder<TDeclaringType>
            Done(ContainerBuilder containerBuilder)
        {
            containerBuilder
                .RegisterType<UnaryFieldMarshaler<TDeclaringType, TMarshaledType>>()
                .WithParameter(new ResolvedParameter(
                    (pi, ctx) => pi.ParameterType == typeof(UnaryCallbacks<TDeclaringType, TMarshaledType>),
                    (pi, ctx) => callbacks))
                .WithParameter(new ResolvedParameter(
                    (pi, ctx) => pi.ParameterType == typeof(MarshalingFeaturesBuilder),
                    (pi, ctx) => marshalingFeatures))
                .Keyed<IFieldMarshaler<TDeclaringType>>(parent.Guid)
                .InstancePerLifetimeScope();

            return parent;
        }

        public UnaryFieldBuilder<TDeclaringType, TMarshaledType>
            WriteFrom(Func<TDeclaringType, TMarshaledType?> getter)
        {
            callbacks.Getter = getter;
            return this;
        }
        public UnaryFieldBuilder<TDeclaringType, TMarshaledType>
            ReadInto(Action<TDeclaringType, TMarshaledType?> setter)
        {
            callbacks.Setter = setter;
            return this;
        }
    }
}
