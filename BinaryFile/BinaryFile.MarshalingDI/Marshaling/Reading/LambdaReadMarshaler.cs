using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Activating;

namespace BinaryFile.MarshalingDI.Marshaling.Reading
{
    public class LambdaReadMarshaler<TMarshaledType> : IReadMarshaler<TMarshaledType>
    {
        public class Config
        {
            public Func<IFeatureSetStack, IDataBuffer, IOffsetStack, (TMarshaledType value, int bytesRead)> reader;
            public int order;
            public Func<IFeatureSetStack, IDataBuffer, IOffsetStack, bool> isFor = ((f, d, o) => true);
        }
        public class Builder
        {
            private Config config = new Config();

            public Builder
                WithOrder(int order)
            {
                config.order = order;
                return this;
            }
            public Builder
                WithReader(Func<IFeatureSetStack, IDataBuffer, IOffsetStack, (TMarshaledType value, int bytesRead)> reader)
            {
                config.reader = reader;
                return this;
            }
            public Builder
                WithCondition(Func<IFeatureSetStack, IDataBuffer, IOffsetStack, bool> isFor)
            {
                config.isFor = isFor;
                return this;
            }

            private List<Type> activationTypes = [];
            public Builder
                AlsoFor<T>()
            {
                var type = typeof(IReadMarshaler<T>);
                activationTypes.Add(type);
                return this;
            }
            public void Register(ContainerBuilder containerBuilder)
            {
                var register = containerBuilder.RegisterType<LambdaReadMarshaler<TMarshaledType>>()
                    .WithParameter(new NamedParameter(nameof(config), config))
                    .As<IReadMarshaler<TMarshaledType>>()
                    .InstancePerLifetimeScope();

                foreach (var type in activationTypes)
                    register = register.As(type);
            }
        }

        private readonly IOffsetStack offsetStack;
        private readonly IDataBuffer dataBuffer;
        private readonly IFeatureSetStack features;
        private readonly Config config;

        public LambdaReadMarshaler(
            IOffsetStack offsetStack,
            IDataBuffer dataBuffer,
            IFeatureSetStack features,
            Config config)
        {
            this.offsetStack = offsetStack;
            this.dataBuffer = dataBuffer;
            this.features = features;
            this.config = config;
        }

        public int Order(EMarshalingType marshalingType) => config.order;

        public bool IsForReading()
        {
            return config.isFor(features, dataBuffer, offsetStack);
        }

        public TMarshaledType Read(out int bytesRead)
        {
            var x = config.reader(features, dataBuffer, offsetStack);
            bytesRead = x.bytesRead;
            return x.value;
        }
    }
}
