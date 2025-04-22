using Autofac.Core;
using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.Marshaling.Collection;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers;
using System.Linq.Expressions;
using ReflectionHelper;
using System.Collections.Generic;
using BinaryDataHelper;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Builders
{
    public partial class CollectionFieldBuilder<TDeclaringType, TMarshaledType>
        : BaseFieldBuilder<TDeclaringType, TMarshaledType, CollectionFieldBuilder<TDeclaringType, TMarshaledType>, CollectionCallbacks<TDeclaringType, TMarshaledType>>
    {
        public CollectionFieldBuilder(ObjectBuilder<TDeclaringType> parent)
            : base(parent)
        {
        }

        public override void Register(ContainerBuilder containerBuilder, Guid objectMarshalerId)
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
        }

        public override ObjectBuilder<TDeclaringType>
            Done() => this.parent;

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
            ReadInto(Action<TDeclaringType, List<(int Offset, TMarshaledType? Value)>, int> setter)
        {
            callbacks.Setter = setter!;
            return this;
        }

        public CollectionFieldBuilder<TDeclaringType, TMarshaledType>
            ForProperty(Expression<Func<TDeclaringType, IEnumerable<TMarshaledType>>> getter)
        {
            callbacks.Getter = getter.Compile();

            var memberType = getter.GetMemberType();
            if (memberType == null)
            {
                throw new Exception($"Auto property failed, provided lambda is not a MemberExpression.");
            }

            var memberInfo = getter.GetMemberInfo();

            if (memberType == typeof(List<TMarshaledType>)
                ||
                memberType == typeof(IList<TMarshaledType>)
                ||
                memberType == typeof(IEnumerable<TMarshaledType>))
            {
                Func<List<(int Offset, TMarshaledType? Value)>, List<TMarshaledType>> converter
                    = (data) => new List<TMarshaledType>(data.Select(x => x.Value));

                var setter = Expressions.CreateConverterSetter<TDeclaringType, List<TMarshaledType>, List<(int Offset, TMarshaledType? Value)>>(memberInfo, converter).Compile();
                callbacks.Setter = (obj, data, bytes) => setter(obj, data);
            }
            else if (memberType == typeof(TMarshaledType[]))
            {
                Func<List<(int Offset, TMarshaledType? Value)>, TMarshaledType[]> converter
                    = (data) => data.Select(x => x.Value).ToArray();

                var setter = Expressions.CreateConverterSetter<TDeclaringType, TMarshaledType[], List<(int Offset, TMarshaledType? Value)>>(memberInfo, converter).Compile();
                callbacks.Setter = (obj, data, bytes) => setter(obj, data);
            }
            else if (memberType == typeof(DistinctList<TMarshaledType>))
            {
                Func<List<(int Offset, TMarshaledType? Value)>, DistinctList<TMarshaledType>> converter
                    = (data) => new DistinctList<TMarshaledType>(data.Select(x => x.Value));

                var setter = Expressions.CreateConverterSetter<TDeclaringType, DistinctList<TMarshaledType>, List<(int Offset, TMarshaledType? Value)>>(memberInfo, converter).Compile();

                callbacks.Setter = (obj, data, bytes) => setter(obj, data);
            }
            else
            {
                throw new Exception($"Unsupported field type of {memberType.FullName}, can't build Setter for auto property.");
            }

            return this;
        }
    }
}
