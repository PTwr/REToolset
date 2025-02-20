using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryDataHelper;
using Autofac;

namespace BinaryFile.MarshalingDI.Marshaling.Activating
{
    //TODO unify with ObjectBuilder
    public class PatternActivatorMarshaler<TMarshaledType> : IActivatorMarshaler<TMarshaledType>
        where TMarshaledType : new()
    {
        private readonly byte?[] pattern;
        private readonly IOffsetStack offsetStack;
        private readonly IDataBuffer data;
        private readonly int order;

        public PatternActivatorMarshaler(
            IOffsetStack offsetStack,
            IDataBuffer data,
            IEnumerable<byte?> pattern, int order = 0)
        {
            this.pattern = pattern.ToArray();
            this.offsetStack = offsetStack;
            this.data = data;
            this.order = order;
        }

        public class Builder
        {
            int order;
            IEnumerable<byte?> pattern = [];
            public Builder
                WithPattern(IEnumerable<byte?> pattern)
            {
                this.pattern = pattern;
                return this;
            }
            public Builder
                WithActivationOrderOf(int order)
            {
                this.order = order; 
                return this;
            }
            private List<Type> activationTypes = [];
            public Builder
                AlsoFor<T>()
            {
                var type = typeof(IActivatorMarshaler<T>);
                activationTypes.Add(type);
                return this;
            }
            public void Register(ContainerBuilder containerBuilder)
            {
                var register = containerBuilder.RegisterType<PatternActivatorMarshaler<TMarshaledType>>()
                    .WithParameter(new NamedParameter(nameof(pattern), pattern))
                    .WithParameter(new NamedParameter(nameof(order), order))
                    .As<IActivatorMarshaler<TMarshaledType>>();

                foreach (var type in activationTypes)
                    register = register.As(type);
            }
        }

        public int Order(EMarshalingType marshalingType) => order;

        public bool IsForActivating()
        {
            return data.AsSpan(offsetStack.CurrentAbsoluteOffset).StartsWith(pattern);
        }

        public TMarshaledType Activate()
        {
            return new();
        }
    }
}
