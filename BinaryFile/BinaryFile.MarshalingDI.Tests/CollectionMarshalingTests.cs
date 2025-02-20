using Autofac;
using Autofac.Features.Metadata;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.DI;
using BinaryFile.MarshalingDI.Marshaling.Collection;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using LanguageExt;
using ReflectionHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Tests
{
    public class CollectionMarshalingTests
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

            ContainerBuilder containerBuilder = new ContainerBuilder();
            containerBuilder
                .WithRequiredServices()
                .WithHelpers()
                .WithPrimitiveMarshalers();

            new LambdaReadMarshaler<A>.Builder()
                .WithReader((h, d, o) => (new A() { X = d[o] }, 1))
                .WithCondition((h, d, o) => d[o] == 0x01)
                .AlsoFor<face>()
                .Register(containerBuilder);
            new LambdaReadMarshaler<B>.Builder()
                .WithReader((h, d, o) => (new B() { X = d[o] }, 1))
                .WithCondition((h, d, o) => d[o] == 0x02)
                .AlsoFor<face>()
                .Register(containerBuilder);
            new LambdaReadMarshaler<C>.Builder()
                .WithReader((h, d, o) => (new C() { X = d[o] }, 1))
                .WithCondition((h, d, o) => d[o] == 0x03)
                .AlsoFor<face>()
                .Register(containerBuilder);
            new LambdaReadMarshaler<_Base>.Builder()
                .WithReader((h, d, o) => (new _Base() { X = d[o] }, 1))
                .WithOrder(int.MaxValue)
                .AlsoFor<face>()
                .Register(containerBuilder);

            var container = containerBuilder.Build();

            container.Resolve<IDataBufferIO>().SetData(binary);

            var colMar = container.Resolve<DefaultCollectionMarshaler>();

            var result = colMar.ListReader<face>();

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
