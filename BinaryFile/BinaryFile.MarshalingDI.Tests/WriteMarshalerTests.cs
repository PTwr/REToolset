using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.ObjectMarshaling;
using BinaryFile.MarshalingDI.TypeMarshaling;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            IDataBuffer dataBuffer = new DefaultDataBuffer([], true);

            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalingMetadata metadata = new DefaultMarshalingMetadata();

            var c = new C()
            {
                X = 7,
            };

            var m1 = new LambdaWriterMarshaler<A>((v, d, m, o) =>
            {
                d[0] = v.X;
                return 1;
            }, 0);
            var m2 = new LambdaWriterMarshaler<B>((v, d, m, o) =>
            {
                d[1] = (byte)(v.X*2);
                return 1;
            }, 0);

            //write marshalers, like mutable readers, are registered to exact type
            //polyph issue is taken care of by type hierarchy crawling in Store
            ContainerBuilder containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(m1)
                .As<IWriteMarshaler<A>>();
            containerBuilder.RegisterInstance(m2)
                .As<IWriteMarshaler<B>>();

            var container = containerBuilder.Build();

            IMarshalerStore store = new DefaultMarshalerStore(container);

            //specifying TMarshaledType different from value.GetType allows to force usage of parent marshaler
            //TODO decide if its a feautre or unnecessary klutter
            //both GetWriteMarshaler and GetMutableReadMarshaler should probably get type from instance not generic
            //but what if instance is null?!
            //marshaling might wan to write something if null, and what if Activator returns null as feature?
            var m11 = store.GetWriteMarshaler<A>(c);
            var m22 = store.GetWriteMarshaler<B>(c);

            Assert.Equal(m1, m11);
            Assert.Equal(m2, m22);

            var m33 = store.GetWriteMarshaler<C>(c);
            //hierarchy traverse should fallback to B for C
            Assert.Equal(m2, m33);

            m11.Write(c, dataBuffer, out _, metadata, offsetStack);
            Assert.Equal(c.X, dataBuffer[0]);
            m22.Write(c, dataBuffer, out _, metadata, offsetStack);
            Assert.Equal(c.X*2, dataBuffer[1]);
        }
    }
}
