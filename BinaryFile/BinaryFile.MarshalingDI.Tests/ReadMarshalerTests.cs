using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.ObjectMarshaling;
using BinaryFile.MarshalingDI.Marshaling;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            IDataBuffer dataBuffer = new DefaultDataBuffer(binary, false);

            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalingMetadata metadata = new DefaultMarshalingMetadata();

            var m1 = new LambdaReadMarshaler<A>((data, meta, stack) => (new A() { X = data.ElementAt(0) }, 1), 0);
            var m2 = new LambdaReadMarshaler<B>((data, meta, stack) => (new B() { X = data.ElementAt(1) }, 1), -1);

            //marshalers should be registered to field types, not exact datatypes
            ContainerBuilder containerBuilder = new ContainerBuilder();
            //immutable marshalers can return child class safely, thus can be assigned to fill parent-type fields
            containerBuilder.RegisterInstance(m1)
                .As<IReadMarshaler<A>, IReadMarshaler<_Base>, IReadMarshaler<face>>();
            containerBuilder.RegisterInstance(m2)
                .As(typeof(IReadMarshaler<B>), typeof(IReadMarshaler<A>), typeof(IReadMarshaler<_Base>), typeof(IReadMarshaler<face>));

            var container = containerBuilder.Build();

            IMarshalerStore store = new DefaultMarshalerStore(container);

            //marshalers should be accessible as its base class/interface
            //exact implementation type is known from Activation
            //field type is required as stop condition for crawling parent types
            var m11 = store.GetReadMarshaler<face, A>(dataBuffer, metadata, offsetStack);
            var m22 = store.GetReadMarshaler<face, B>(dataBuffer, metadata, offsetStack);

            Assert.Equal(m1, m11);
            Assert.Equal(m2, m22);

            //immutable read should never be fetched with TFieldType different from TMarshaledType by real use through FieldDescriptor?
            //but it should be possible to register descendant reader to simulate how activators can be overriden!
            var m33 = store.GetReadMarshaler<face, C>(dataBuffer, metadata, offsetStack);

            //hierarchy traverse should fallback to B for C
            Assert.Equal(m2, m33);

            var r1 = m11.Read(dataBuffer, out _, metadata, offsetStack);
            var r2 = m22.Read(dataBuffer, out _, metadata, offsetStack);
            var r3 = m33.Read(dataBuffer, out _, metadata, offsetStack);

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
            IDataBuffer dataBuffer = new DefaultDataBuffer(binary, false);

            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalingMetadata metadata = new DefaultMarshalingMetadata();

            var m1 = new LambdaMutableReadMarshaler<A>((v, data, meta, stack) =>
            {
                v.X = data.ElementAt(0);
                return 1;
            }, 0);
            var m2 = new LambdaMutableReadMarshaler<B>((v, data, meta, stack) =>
            {
                v.X = data.ElementAt(1);
                return 1;
            }, 0);

            //marshalers should be registered to field types, not exact datatypes
            //mutable marshalers can only handle same or child types, thus can't be asigned to parent-type fields
            //parent-type marshalers will be fetched through type-hierarchy-crawl
            ContainerBuilder containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(m1)
                .As<IMutableReadMarshaler<A>>();
            containerBuilder.RegisterInstance(m2)
                .As<IMutableReadMarshaler<B>>();

            var container = containerBuilder.Build();

            IMarshalerStore store = new DefaultMarshalerStore(container);

            //MutableMarshalers should respond primarily to its exact type
            //no need to split TFieldType from TMarshaledType as exact type is known from Activation
            var m11 = store.GetMutableReadMarshaler<A>(dataBuffer, metadata, offsetStack);
            var m22 = store.GetMutableReadMarshaler<B>(dataBuffer, metadata, offsetStack);

            Assert.Equal(m1, m11);
            Assert.Equal(m2, m22);

            //mutable marshalers, like write marshalers, should also be able to handle child classes
            //inheritance/polymorph is taken care by type-hierarchy-crawl
            var m33 = store.GetMutableReadMarshaler<C>(dataBuffer, metadata, offsetStack);

            //hierarchy traverse should fallback to B for C
            Assert.Equal(m2, m33);

            var r1 = new C();
            m11.Read(r1, dataBuffer, out _, metadata, offsetStack);
            var r2 = new C();
            m22.Read(r2, dataBuffer, out _, metadata, offsetStack);
            var r3 = new C();
            m33.Read(r3, dataBuffer, out _, metadata, offsetStack);

            Assert.Equal(0x01, r1.X);
            Assert.Equal(0x02, r2.X);
            Assert.Equal(0x02, r3.X);
        }
    }
}
