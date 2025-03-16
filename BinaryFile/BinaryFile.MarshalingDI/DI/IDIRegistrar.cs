using Autofac;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Collection;
using BinaryFile.MarshalingDI.Marshaling.Helpers;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;
using BinaryFile.MarshalingDI.PrimitiveMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.DI
{
    public interface IDIRegistrar
    {
        void Register(ContainerBuilder containerBuilder);
    }
    public sealed class PrimitiveMarshalingRegistrar : IDIRegistrar
    {
        public void Register(ContainerBuilder containerBuilder)
        {

        }
    }
    public static class DefaultRegistrars
    {
        public static ContainerBuilder WithRequiredServices(this ContainerBuilder containerBuilder, bool useOptimizedServices = true)
        {
            containerBuilder.RegisterType<DefaultOffsetStack>()
                .As<IOffsetStack>()
                .InstancePerLifetimeScope();
            containerBuilder.RegisterType<DefaultDataBuffer>()
                .As<IDataBuffer>()
                .As<IDataBufferIO>()
                .InstancePerLifetimeScope();
            containerBuilder.RegisterType<FeatureSetStack>()
                .As<IFeatureSetStack>()
                .InstancePerLifetimeScope();

            if (useOptimizedServices)
            {
                containerBuilder.RegisterType<CachedMarshalerStore>()
                    .As<IMarshalerStore>()
                    .InstancePerLifetimeScope();
            }
            else
            {
                containerBuilder.RegisterType<DefaultMarshalerStore>()
                    .As<IMarshalerStore>()
                    .InstancePerLifetimeScope();
            }

            return containerBuilder;
        }
        public static ContainerBuilder WithHelpers(this ContainerBuilder containerBuilder)
        {
            containerBuilder
                .RegisterType<ReadHelper>()
                .InstancePerLifetimeScope();
            containerBuilder
                .RegisterType<WriteHelper>()
                .InstancePerLifetimeScope();
            containerBuilder
                .RegisterType<DefaultCollectionMarshaler>()
                .As<DefaultCollectionMarshaler>()
                .InstancePerLifetimeScope();

            return containerBuilder;
        }
        public static ContainerBuilder WithPrimitiveMarshalers(this ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<IntegerMarshaler>()
                .As<IReadMarshaler<byte>>()
                .As<IWriteMarshaler<byte>>()
                .As<IReadMarshaler<Int32>>()
                .As<IWriteMarshaler<Int32>>()
                .As<IReadMarshaler<ushort>>()
                .As<IWriteMarshaler<ushort>>()
                .As<IReadMarshaler<short>>()
                .As<IWriteMarshaler<short>>()
                .InstancePerLifetimeScope();
            containerBuilder.RegisterType<StringMarshaler>()
                .As<IReadMarshaler<string>>()
                .As<IWriteMarshaler<string>>()
                .InstancePerLifetimeScope();

            return containerBuilder;
        }
    }
}
