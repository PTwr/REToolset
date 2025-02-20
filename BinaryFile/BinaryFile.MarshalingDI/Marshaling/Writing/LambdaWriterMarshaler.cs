using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Reading;

namespace BinaryFile.MarshalingDI.Marshaling.Writing
{
    public class LambdaWriterMarshaler<TMarshaledType> : IWriteMarshaler<TMarshaledType>
    {
        public class Config
        {
            public Func<TMarshaledType, IHierarchicalFeatureSet, IDataBuffer, IOffsetStack, int> writer;
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
                WithWriter(Func<TMarshaledType, IHierarchicalFeatureSet, IDataBuffer, IOffsetStack, int> writer)
            {
                config.writer = writer;
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
                var register = containerBuilder.RegisterType<LambdaWriterMarshaler<TMarshaledType>>()
                    .WithParameter(new NamedParameter(nameof(config), config))
                    .As<IWriteMarshaler<TMarshaledType>>()
                    .InstancePerLifetimeScope();

                foreach (var type in activationTypes)
                    register = register.As(type);
            }
        }

        private readonly IOffsetStack offsetStack;
        private readonly IDataBuffer dataBuffer;
        private readonly IHierarchicalFeatureSet features;
        private readonly Config config;

        public LambdaWriterMarshaler(
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

        public bool IsForWriting()
        {
            return config.isFor(features, dataBuffer, offsetStack);
        }

        public void Write(TMarshaledType value, out int bytesWrote)
        {
            bytesWrote = config.writer(value, features, dataBuffer, offsetStack);
        }
    }
}
