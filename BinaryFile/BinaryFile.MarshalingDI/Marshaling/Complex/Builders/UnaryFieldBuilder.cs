using Autofac;
using Autofac.Core;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers;
using BinaryFile.MarshalingDI.Marshaling.Helpers;
using ReflectionHelper;
using System.Linq.Expressions;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    public partial class UnaryFieldBuilder<TDeclaringType, TMarshaledType>
        : BaseFieldBuilder<TDeclaringType, TMarshaledType, UnaryFieldBuilder<TDeclaringType, TMarshaledType>, UnaryCallbacks<TDeclaringType, TMarshaledType>>
    {
        protected internal UnaryFieldBuilder(ObjectBuilder<TDeclaringType> parent)
            : base(parent)
        {
        }

        public override void Register(ContainerBuilder containerBuilder, Guid objectMarshalerId)
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
        }

        public override ObjectBuilder<TDeclaringType>
            Done() => this.parent;

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

        public UnaryFieldBuilder<TDeclaringType, TMarshaledType>
            ForProperty(Expression<Func<TDeclaringType, TMarshaledType?>> getter)
        {
            callbacks.Getter = getter.Compile();

            callbacks.Setter = getter.GenerateToSetter().Compile();

            return this;
        }
        public UnaryFieldBuilder<TDeclaringType, TMarshaledType>
            WithExpectedValueOf(TMarshaledType expectedValue)
        {
            Func<ILifetimeScope, bool> validator = (c) =>
            {
                if (callbacks.Getter == null) throw new Exception($"Cannot run automatic {nameof(WithExpectedValueOf)} due to missing Getter for {c.Resolve<IFeatureSetStack>().GetDebugInfo()}");
                return EqualityComparer<TMarshaledType>.Default.Equals(callbacks.Getter(c.Resolve<IFeatureSetStack>().GetCurentObject<TDeclaringType>()), expectedValue);
            };

            return this
                .WithBeforeWriteValidator(validator);
        }
    }
}
