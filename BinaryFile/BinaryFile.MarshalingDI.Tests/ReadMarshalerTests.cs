using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.DI;

namespace BinaryFile.MarshalingDI.Tests
{
    public class ReadMarshalerTests
    {
        interface face { byte X { get; set; } }
        class _Base : face { public byte X { get; set; } }
        class A : _Base { }
        class B : A { }
        class C : B { }

        [Fact]
        public void ImmutableMarshalerTest()
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
                .WithReader((h, d, o) => (new A() { X = d[0] }, -1))
                .AlsoFor<face>().AlsoFor<_Base>()
                .Register(containerBuilder);
            new LambdaReadMarshaler<B>.Builder()
                .WithReader((h, d, o) => (new B() { X = d[1] }, 0))
                .AlsoFor<face>().AlsoFor<_Base>().AlsoFor<A>()
                .Register(containerBuilder);

            var container = containerBuilder.Build();

            container.Resolve<IDataBufferIO>().SetData(binary);

            IMarshalerStore store = new DefaultMarshalerStore(container);

            //marshalers should be accessible as its base class/interface
            //exact implementation type is known from Activation
            //field type is required as stop condition for crawling parent types
            var m11 = store.GetReadMarshaler<face, A>();
            var m22 = store.GetReadMarshaler<face, B>();

            Assert.IsType<LambdaReadMarshaler<A>>(m11);
            Assert.IsType<LambdaReadMarshaler<B>>(m22);

            //immutable read should never be fetched with TFieldType different from TMarshaledType by real use through FieldDescriptor?
            //but it should be possible to register descendant reader to simulate how activators can be overriden!
            var m33 = store.GetReadMarshaler<face, C>();

            //hierarchy traverse should fallback to B for C
            Assert.IsType<LambdaReadMarshaler<B>>(m33);

            var r1 = m11.Read(out _);
            var r2 = m22.Read(out _);
            var r3 = m33.Read(out _);

            Assert.IsType<A>(r1);
            Assert.IsType<B>(r2);
            Assert.IsType<B>(r3); //fallback for missing Marshaler<C>, won't happen in proper usage through FieldDescriptors

            Assert.Equal(0x01, r1.X);
            Assert.Equal(0x02, r2.X);
            Assert.Equal(0x02, r3.X);
        }


        [Fact]
        public void MutableMarshalerTest()
        {
            byte[] binary = [
                0x01, 0x02, 0x03, 0x04,
                ];

            ContainerBuilder containerBuilder = new ContainerBuilder();
            containerBuilder
                .WithRequiredServices()
                .WithHelpers()
                .WithPrimitiveMarshalers();

            new LambdaMutableReadMarshaler<A>.Builder()
                .WithOrder(0)
                .WithReader((v, h, d, o) =>
                {
                    v.X = d[0];
                    return 1;
                })
                .Register(containerBuilder);
            new LambdaMutableReadMarshaler<B>.Builder()
                .WithOrder(0)
                .WithReader((v, h, d, o) =>
                {
                    v.X = d[1];
                    return 1;
                })
                .Register(containerBuilder);

            //marshalers should be registered to field types, not exact datatypes
            //mutable marshalers can only handle same or child types, thus can't be asigned to parent-type fields
            //parent-type marshalers will be fetched through type-hierarchy-crawl

            var container = containerBuilder.Build();

            IMarshalerStore store = new DefaultMarshalerStore(container);

            //MutableMarshalers should respond primarily to its exact type
            //no need to split TFieldType from TMarshaledType as exact type is known from Activation
            var m11 = store.GetMutableReadMarshaler<A>();
            var m22 = store.GetMutableReadMarshaler<B>();

            Assert.IsType<LambdaMutableReadMarshaler<A>>(m11);
            Assert.IsType<LambdaMutableReadMarshaler<B>>(m22);

            //mutable marshalers, like write marshalers, should also be able to handle child classes
            //inheritance/polymorph is taken care by type-hierarchy-crawl
            var m33 = store.GetMutableReadMarshaler<C>();

            //hierarchy traverse should fallback to B for C
            Assert.IsType<LambdaMutableReadMarshaler<B>>(m33);

            var r1 = new C();
            m11.Read(r1, out _);
            var r2 = new C();
            m22.Read(r2, out _);
            var r3 = new C();
            m33.Read(r3, out _);

            Assert.Equal(0x01, r1.X);
            Assert.Equal(0x02, r2.X);
            Assert.Equal(0x02, r3.X);
        }
    }
}
