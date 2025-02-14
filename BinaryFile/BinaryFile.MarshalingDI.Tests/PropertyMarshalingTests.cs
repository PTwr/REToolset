using Autofac;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Collection;
using BinaryFile.MarshalingDI.Marshaling.Complex;
using BinaryFile.MarshalingDI.Marshaling.Helpers;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;
using BinaryFile.MarshalingDI.PrimitiveMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Tests
{
    public class PropertyMarshalingTests
    {
        byte[] binary = [0x01, 0x02, 0x03, 0x04];

        class Foo
        {
            public byte A { get; set; }
            public byte B { get; set; }
            public byte C { get; set; }
            public byte D { get; set; }
        }

        IContainer Setup()
        {
            ContainerBuilder containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterType<DefaultMarshalerStore>()
                .As<IMarshalerStore>();
            containerBuilder.RegisterType<DefaultCollectionMarshaler>()
                .As<DefaultCollectionMarshaler>();
            containerBuilder.RegisterType<IntegerMarshaler>()
                .As<IReadMarshaler<byte>>()
                .As<IWriteMarshaler<byte>>();

            new ObjectBuilder<Foo>()
                .WithDefaultActivator((x) => new Foo())
                .WithFieldOf<byte>()
                .AtOffset((foo) => (0, OffsetRelation.Segment))
                .ReadInto((foo, x) => foo.A = x)
                .WriteFrom((foo) => foo.A)
                .Done()
                .WithFieldOf<byte>()
                .AtOffset((foo) => (1, OffsetRelation.Segment))
                .ReadInto((foo, x) => foo.B = x)
                .WriteFrom((foo) => foo.B)
                .Done()
                .WithFieldOf<byte>()
                .AtOffset((foo) => (2, OffsetRelation.Segment))
                .ReadInto((foo, x) => foo.C = x)
                .WriteFrom((foo) => foo.C)
                .Done()
                .WithFieldOf<byte>()
                .AtOffset((foo) => (3, OffsetRelation.Segment))
                .ReadInto((foo, x) => foo.D = x)
                .WriteFrom((foo) => foo.D)
                .Done()
                .RegisterInDI(containerBuilder);


            return containerBuilder.Build();
        }

        [Fact]
        public void ReadProperties()
        {
            var container = Setup();

            IDataBuffer dataBuffer = new DefaultDataBuffer(binary, false);
            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalingMetadata metadata = new DefaultMarshalingMetadata();

            var foo = ReadHelper.Read<Foo>(container.Resolve<IMarshalerStore>(), null, dataBuffer, metadata, offsetStack, out _);

            Assert.NotNull(foo);

            Assert.Equal(1, foo.A);
            Assert.Equal(2, foo.B);
            Assert.Equal(3, foo.C);
            Assert.Equal(4, foo.D);
        }
        [Fact]
        public void WriteProperties()
        {
            var container = Setup();

            IDataBuffer dataBuffer = new DefaultDataBuffer([], true);
            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalingMetadata metadata = new DefaultMarshalingMetadata();

            var foo = new Foo()
            {
                A = 5,
                B = 6,
                C = 7,
                D = 8,
            };

            WriteHelper.Write<Foo>(container.Resolve<IMarshalerStore>(), foo, dataBuffer, metadata, offsetStack, out _);

            var bin = dataBuffer.AsSpan().ToArray();

            Assert.Equal([5, 6, 7, 8], bin);
        }
    }
}
