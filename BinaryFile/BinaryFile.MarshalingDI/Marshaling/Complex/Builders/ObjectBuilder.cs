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
using System.Linq.Expressions;
using System.Net.Http.Headers;
using ReflectionHelper;
using System.Text;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    public partial class ObjectBuilder<TDeclaringType>
        : BaseBuilder<TDeclaringType, ObjectBuilder<TDeclaringType>, ObjectCallbacks<TDeclaringType>>
    {
        protected List<Type> additionalTypes = [];
        protected List<IFieldBuilder> fieldBuilders = [];

        public Guid Guid { get; } = Guid.NewGuid();
        public void RegisterInDI(ContainerBuilder containerBuilder)
        {
            //TODO rethink, it might be desireable for activation to automagically occur whenever possible without reigstration of default activator
            if (callbacks.DefaultActivator is null && !callbacks.Activators.Any())
            {
                var defaultCtor = ActivationHelper.PrepareActivationLambda<TDeclaringType>();
                callbacks.DefaultActivator = (ILifetimeScope c) => defaultCtor();
            }

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

            foreach (var registrar in fieldBuilders) registrar.Register(containerBuilder, Guid);
        }

        public ObjectBuilder<TDeclaringType>
            ForBytePatternOf(byte?[] pattern)
        {
            callbacks.IsForActivating = (di) =>
                di.Resolve<IDataBuffer>().AsSpan(di.Resolve<IOffsetStack>().CurrentAbsoluteOffset).StartsWith(pattern);
            callbacks.IsForReading = (di) =>
                di.Resolve<IDataBuffer>().AsSpan(di.Resolve<IOffsetStack>().CurrentAbsoluteOffset).StartsWith(pattern);
            return this;
        }
        public ObjectBuilder<TDeclaringType>
            ForBytePatternOf(string str, Encoding? encoding = null)
        {
            encoding ??= Encoding.ASCII;
            var pattern = encoding.GetBytes(str);
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
            WithByteLengthOf(Func<TDeclaringType, int> byteLength)
            => this.WithReadByteLengthOf(byteLength).WithWriteByteLengthOf(byteLength);
        public ObjectBuilder<TDeclaringType>
            WithByteLengthOf(int byteLength)
            => this.WithByteLengthOf((TDeclaringType x) => byteLength);

        public ObjectBuilder<TDeclaringType>
            WithReadByteLengthOf(Func<TDeclaringType, int> byteLength)
        {
            callbacks.BytesRead = byteLength;
            return this;
        }
        public ObjectBuilder<TDeclaringType>
            WithReadByteLengthOf(int byteLength) 
            => this.WithReadByteLengthOf((TDeclaringType x) => byteLength);

        public ObjectBuilder<TDeclaringType>
            WithWriteByteLengthOf(Func<TDeclaringType, int> byteLength)
        {
            callbacks.BytesWrote = byteLength;
            return this;
        }
        public ObjectBuilder<TDeclaringType>
            WithWriteByteLengthOf(int byteLength)
            => this.WithWriteByteLengthOf((TDeclaringType x) => byteLength);

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
            WithActivator(Func<ILifetimeScope, (bool activated, TDeclaringType? value)> activator)
        {
            callbacks.Activators.Add(activator);
            return this;
        }
        public ObjectBuilder<TDeclaringType>
            WithDefaultActivator<TParent>(Func<TParent, TDeclaringType?> activator)
        {
            var func = (ILifetimeScope c) => activator(c
                .Resolve<IFeatureSetStack>()
                .GetParent<TParent>());
            callbacks.DefaultActivator = func;
            return this;
        }
        public ObjectBuilder<TDeclaringType>
            WithActivator<TParent>(Func<TParent, TDeclaringType?> activator)
        {
            var func = (ILifetimeScope c) =>
            {
                if (c.Resolve<IFeatureSetStack>().TryGetParent<TParent>(out var parent))
                {
                    var value = activator(parent);
                    return (true, value);
                }
                return (false, default);
            };
            callbacks.Activators.Add(func);
            return this;
        }
        public ObjectBuilder<TDeclaringType>
            WithDefaultActivator(Func<TDeclaringType?> activator)
        {
            callbacks.DefaultActivator = (ILifetimeScope c) => activator();
            return this;
        }
        public ObjectBuilder<TDeclaringType>
            WithActivator(Func<(bool activated, TDeclaringType? value)> activator)
        {
            callbacks.Activators.Add((ILifetimeScope c) => activator());
            return this;
        }

        public UnaryFieldBuilder<TDeclaringType, string>
            WithMagicString(string value, int offset = 0, OffsetRelation offsetRelation = OffsetRelation.Segment, Encoding? encoding = null)
        {
            var builder = WithMagicString(value, (x) => (offset, offsetRelation), encoding);

            return builder;
        }
        public UnaryFieldBuilder<TDeclaringType, string>
            WithMagicString(string value, Func<TDeclaringType, (int offset, OffsetRelation relation)> offsetCalculator, Encoding? encoding = null)
        {
            encoding ??= Encoding.ASCII;
            var builder = WithMagicOf<string>(value, offsetCalculator)
                .WithReadWriteMetadata(EStringLengthStyle.FixedLength)
                .WithReadWriteMetadata(encoding)
                .WithReadWriteMetadata(value.Length, nameof(EMetadataNames.StringLength));

            return builder;
        }
        public UnaryFieldBuilder<TDeclaringType, TMarshaledType>
            WithMagicOf<TMarshaledType>(TMarshaledType value, int offset, OffsetRelation offsetRelation = OffsetRelation.Segment)
        {
            return WithMagicOf<TMarshaledType>(value, (x) => (offset, offsetRelation));
        }
        public UnaryFieldBuilder<TDeclaringType, TMarshaledType>
            WithMagicOf<TMarshaledType>(TMarshaledType value, Func<TDeclaringType, (int offset, OffsetRelation relation)> offsetCalculator)
        {
            var builder = WithFieldOf<TMarshaledType>()
                .ReadInto((obj, v) => {
                    if (!EqualityComparer<TMarshaledType>.Default.Equals(v, value))
                    {
                        throw new InvalidDataException($"Expected Magic value of '{value}'. Found '{v}'.");
                    }
                })
                .WriteFrom((x) => value)
                .AtOffset(offsetCalculator);

            return builder;
        }

        public UnaryFieldBuilder<TDeclaringType, TMarshaledType>
            WithFieldOf<TMarshaledType>()
        {
            var builder = new UnaryFieldBuilder<TDeclaringType, TMarshaledType>(this);

            fieldBuilders.Add(builder);

            return builder;
        }
        public UnaryFieldBuilder<TDeclaringType, TMarshaledType>
            WithField<TMarshaledType>(Expression<Func<TDeclaringType, TMarshaledType?>> getter, int offset, OffsetRelation offsetRelation = OffsetRelation.Segment)
        {
            var builder =
                new UnaryFieldBuilder<TDeclaringType, TMarshaledType>(this)
                .ForProperty(getter)
                .AtOffset(offset, offsetRelation);

            fieldBuilders.Add(builder);

            return builder;
        }
        public UnaryFieldBuilder<TDeclaringType, TMarshaledType>
            WithField<TMarshaledType>(Expression<Func<TDeclaringType, TMarshaledType?>> getter, Func<TDeclaringType, (int offset, OffsetRelation relation)> offsetCalculator)
        {
            var builder =
                new UnaryFieldBuilder<TDeclaringType, TMarshaledType>(this)
                .ForProperty(getter)
                .AtOffset(offsetCalculator);

            fieldBuilders.Add(builder);

            return builder;
        }
        public CollectionFieldBuilder<TDeclaringType, TMarshaledType>
            WithCollectionOf<TMarshaledType>()
        {
            //TODO fallback to normal marshaling if marshaler for specific collection type is registered? Huge optimization for stuff like byte[]
            //TODO unifying unary field and collections would suck and pollute fluent with unnecessary config methods, keep separate?
            var builder = new CollectionFieldBuilder<TDeclaringType, TMarshaledType>(this);

            fieldBuilders.Add(builder);

            return builder;
        }
        public CollectionFieldBuilder<TDeclaringType, TMarshaledType>
            WithCollection<TMarshaledType>(Expression<Func<TDeclaringType, IEnumerable<TMarshaledType>>> getter)
        {
            //TODO fallback to normal marshaling if marshaler for specific collection type is registered? Huge optimization for stuff like byte[]
            //TODO unifying unary field and collections would suck and pollute fluent with unnecessary config methods, keep separate?
            var builder = new CollectionFieldBuilder<TDeclaringType, TMarshaledType>(this);
            builder = builder.ForProperty(getter);

            fieldBuilders.Add(builder);

            return builder;
        }
        public CollectionFieldBuilder<TDeclaringType, TMarshaledType>
            WithCollection<TMarshaledType>(Expression<Func<TDeclaringType, IEnumerable<TMarshaledType>>> getter, int offset, OffsetRelation offsetRelation = OffsetRelation.Segment)
        {
            var builder =
                new CollectionFieldBuilder<TDeclaringType, TMarshaledType>(this)
                .ForProperty(getter)
                .AtOffset(offset, offsetRelation);

            fieldBuilders.Add(builder);

            return builder;
        }
        public CollectionFieldBuilder<TDeclaringType, TMarshaledType>
            WithCollection<TMarshaledType>(Expression<Func<TDeclaringType, IEnumerable<TMarshaledType>>> getter, Func<TDeclaringType, int> offsetCalculator, OffsetRelation offsetRelation = OffsetRelation.Segment)
        {
            var builder =
                new CollectionFieldBuilder<TDeclaringType, TMarshaledType>(this)
                .ForProperty(getter)
                .AtOffset(offsetCalculator, offsetRelation);

            fieldBuilders.Add(builder);

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
