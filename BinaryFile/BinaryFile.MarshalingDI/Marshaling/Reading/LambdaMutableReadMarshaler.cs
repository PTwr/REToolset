using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;

namespace BinaryFile.MarshalingDI.Marshaling.Reading
{
    public class LambdaMutableReadMarshaler<TMarshaledType> : IMutableReadMarshaler<TMarshaledType>
        where TMarshaledType : class
    {
        public class Config
        {
            public Func<TMarshaledType, IHierarchicalFeatureSet, IDataBuffer, IOffsetStack, int> reader;
            public int order;
            public Func<IHierarchicalFeatureSet, IDataBuffer, IOffsetStack, bool> isFor = ((f, d, o) => true);
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
                WithReader(Func<TMarshaledType, IHierarchicalFeatureSet, IDataBuffer, IOffsetStack, int> reader)
            {
                config.reader = reader;
                return this;
            }
            public Builder
                WithCondition(Func<IHierarchicalFeatureSet, IDataBuffer, IOffsetStack, bool> isFor)
            {
                config.isFor = isFor;
                return this;
            }

            private List<Type> activationTypes = [];
            public Builder
                AlsoFor<T>()
            {
                var type = typeof(IMutableReadMarshaler<T>);
                activationTypes.Add(type);
                return this;
            }
            public void Register(ContainerBuilder containerBuilder)
            {
                var register = containerBuilder.RegisterType<LambdaMutableReadMarshaler<TMarshaledType>>()
                    .WithParameter(new NamedParameter(nameof(config), config))
                    .As<IMutableReadMarshaler<TMarshaledType>>()
                    .InstancePerLifetimeScope();

                foreach (var type in activationTypes)
                    register = register.As(type);
            }
        }

        private readonly IOffsetStack offsetStack;
        private readonly IDataBuffer dataBuffer;
        private readonly IHierarchicalFeatureSet features;
        private readonly Config config;

        public LambdaMutableReadMarshaler(
            IOffsetStack offsetStack,
            IDataBuffer dataBuffer,
            IHierarchicalFeatureSet features,
            Config config)
        {
            this.offsetStack = offsetStack;
            this.dataBuffer = dataBuffer;
            this.features = features;
            this.config = config;
        }

        public int Order(EMarshalingType marshalingType) => config.order;

        public bool IsForMutableReading()
        {
            return config.isFor(features, dataBuffer, offsetStack);
        }

        public void Read(TMarshaledType value, out int bytesRead)
        {
            bytesRead = config.reader(value, features, dataBuffer, offsetStack);
        }
    }
}
