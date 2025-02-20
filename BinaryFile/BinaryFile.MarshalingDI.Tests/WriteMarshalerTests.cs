using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling;
using BinaryFile.MarshalingDI.Marshaling.Writing;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.DI;
using BinaryFile.MarshalingDI.Marshaling.Helpers;

namespace BinaryFile.MarshalingDI.Tests
{
    public class WriteMarshalerTests
    {
        interface face { byte X { get; set; } }
        class _Base : face { public byte X { get; set; } }
        class A : _Base { }
        class B : A { }
        class C : B { }

        [Fact]
        public void PolyphTest()
        {

            var c = new C()
            {
                X = 7,
            };

            ContainerBuilder containerBuilder = new ContainerBuilder();
            containerBuilder
                .WithRequiredServices()
                .WithHelpers()
                .WithPrimitiveMarshalers();

            new LambdaWriterMarshaler<A>.Builder()
                .WithWriter((v, h, d, o) =>
                {
                    d[0] = v.X;
                    return 1;
                })
                .WithOrder(0)
                .Register(containerBuilder);
            new LambdaWriterMarshaler<B>.Builder()
                .WithWriter((v, h, d, o) =>
                {
                    d[1] = (byte)(v.X*2);
                    return 1;
                })
                .WithOrder(0)
                .Register(containerBuilder);

            //write marshalers, like mutable readers, are registered to exact type
            //polyph issue is taken care of by type hierarchy crawling in Store

            var container = containerBuilder.Build();

            container.Resolve<IDataBufferIO>().EnableResize();
            var store = container.Resolve<IMarshalerStore>();

            //specifying TMarshaledType different from value.GetType allows to force usage of parent marshaler
            //TODO decide if its a feautre or unnecessary klutter
            //both GetWriteMarshaler and GetMutableReadMarshaler should probably get type from instance not generic
            //but what if instance is null?!
            //marshaling might wan to write something if null, and what if Activator returns null as feature?
            var m11 = store.GetWriteMarshaler<A>(c);
            var m22 = store.GetWriteMarshaler<B>(c);

            Assert.IsType<LambdaWriterMarshaler<A>>(m11);
            Assert.IsType<LambdaWriterMarshaler<B>>(m22);

            var m33 = store.GetWriteMarshaler<C>(c);
            //hierarchy traverse should fallback to B for C);

            m11.Write(c, out _);
            var resultBin1 = container.Resolve<IDataBufferIO>().GetData();
            Assert.Equal(c.X, resultBin1[0]);
            m22.Write(c, out _);
            var resultBin2 = container.Resolve<IDataBufferIO>().GetData();
            Assert.Equal(c.X*2, resultBin2[1]);
        }
    }
}
