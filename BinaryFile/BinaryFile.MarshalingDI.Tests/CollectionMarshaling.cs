using Autofac;
using Autofac.Features.Metadata;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Collection;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.ObjectMarshaling;
using ReflectionHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Tests
{
    public class CollectionMarshaling
    {
        interface face { byte X { get; set; } }
        class _Base : face { public byte X { get; set; } }
        class A : _Base { }
        class B : A { }
        class C : B { }

        [Fact]
        public void CollectionMarshalerResolve()
        {
            byte[] binary = [
                0x01, 0x02, 0x03, 0x04,
                ];

            IMarshalerStore store = null;
            var m1 = new LambdaReadMarshaler<A>((data, meta, stack) =>
            {
                return (new A() { X = data[stack] }, 1);
            }, 0, (d, m, o) => d[o] == 0x01);
            var m2 = new LambdaReadMarshaler<B>((data, meta, stack) =>
            {
                return (new B() { X = data[stack] }, 1);
            }, 0, (d, m, o) => d[o] == 0x02);
            var m3 = new LambdaReadMarshaler<C>((data, meta, stack) =>
            {
                return (new C() { X = data[stack] }, 1);
            }, 0, (d, m, o) => d[o] == 0x03);
            var m4 = new LambdaReadMarshaler<_Base>((data, meta, stack) =>
            {
                return (new _Base() { X = data[stack] }, 1);
            }, int.MinValue, null);

            ContainerBuilder containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterInstance(m1)
                .As<IReadMarshaler<face>>();
            containerBuilder.RegisterInstance(m2)
                .As<IReadMarshaler<face>>();
            containerBuilder.RegisterInstance(m3)
                .As<IReadMarshaler<face>>();
            containerBuilder.RegisterInstance(m4)
                .As<IReadMarshaler<face>>();


            containerBuilder.RegisterType<DefaultMarshalerStore>()
                .As<IMarshalerStore>();
            containerBuilder.RegisterType<DefaultCollectionMarshaler>()
                .As<DefaultCollectionMarshaler>();

            var container = containerBuilder.Build();

            IDataBuffer dataBuffer = new DefaultDataBuffer(binary, false);
            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalingMetadata metadata = new DefaultMarshalingMetadata();

            var colMar = container.Resolve<DefaultCollectionMarshaler>();

            var result = colMar.ListReader<face>(dataBuffer, metadata, offsetStack, null);

            Assert.Equal(4, result.bytesRead);
            Assert.Equal(4, result.data.Count);

            Assert.IsType<A>(result.data[0].Value);
            Assert.IsType<B>(result.data[1].Value);
            Assert.IsType<C>(result.data[2].Value);
            Assert.IsType<_Base>(result.data[3].Value);
        }
        [Fact]
        public void PrimitiveCollectionRead()
        {

        }
        [Fact]
        public void PrimitiveCollectionWrite()
        {

        }
        [Fact]
        public void ObjectCollectionRead()
        {

        }
        [Fact]
        public void ObjectCollectionWrite()
        {

        }
    }
}
