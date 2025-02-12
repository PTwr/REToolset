using Autofac;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Collection;
using BinaryFile.MarshalingDI.Marshaling.Complex;
using BinaryFile.MarshalingDI.Marshaling.Reading;
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
                .As<IReadMarshaler<byte>>();

            //TODO ObjectMarshaler also needs Builder which will register lambda activator in DI
            containerBuilder.Register((ctx) =>
            {
                var store = ctx.Resolve<IMarshalerStore>();
                var objMarshaler = new ObjectMarshaler<Foo>(store);

                new ObjectMarshaler<Foo>.FieldMarshaler<byte>.Builder()
                    .Into((foo, x) => foo.A = x)
                    .AtOffset((foo) => (0, OffsetRelation.Segment))
                    .Register(objMarshaler);
                new ObjectMarshaler<Foo>.FieldMarshaler<byte>.Builder()
                    .Into((foo, x) => foo.B = x)
                    .AtOffset((foo) => (1, OffsetRelation.Segment))
                    .Register(objMarshaler);
                new ObjectMarshaler<Foo>.FieldMarshaler<byte>.Builder()
                    .Into((foo, x) => foo.C = x)
                    .AtOffset((foo) => (2, OffsetRelation.Segment))
                    .Register(objMarshaler);
                new ObjectMarshaler<Foo>.FieldMarshaler<byte>.Builder()
                    .Into((foo, x) => foo.D = x)
                    .AtOffset((foo) => (3, OffsetRelation.Segment))
                    .Register(objMarshaler);

                return objMarshaler;
            }).As<ObjectMarshaler<Foo>>();


            return containerBuilder.Build();
        }

        [Fact]
        public void ReadProperties()
        {
            var container = Setup();

            var marshaler = container.Resolve<ObjectMarshaler<Foo>>();

            IDataBuffer dataBuffer = new DefaultDataBuffer(binary, false);
            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalingMetadata metadata = new DefaultMarshalingMetadata();

            var foo = new Foo();
            marshaler.Read(foo, dataBuffer, metadata, offsetStack);

            Assert.Equal(1, foo.A);
            Assert.Equal(2, foo.B);
            Assert.Equal(3, foo.C);
            Assert.Equal(4, foo.D);
        }
        [Fact]
        public void WriteProperties()
        {

        }
    }
}
