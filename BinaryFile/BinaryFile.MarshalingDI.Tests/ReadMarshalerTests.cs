using Autofac;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.ObjectMarshaling;
using BinaryFile.MarshalingDI.TypeMarshaling;
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
            IDataBuffer dataBuffer = new DefaultDataBuffer(binary, true);

            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalingMetadata metadata = new DefaultMarshalingMetadata();

            var m1 = new LambdaReadMarshaler<A>((data, meta, stack) => (new A() { X = data.ElementAt(0) }, 1), 0);
            var m2 = new LambdaReadMarshaler<B>((data, meta, stack) => (new B() { X = data.ElementAt(1) }, 1), -1);

            //marshalers should be registered to field types, not exact datatypes
            ContainerBuilder containerBuilder = new ContainerBuilder();
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
            Assert.IsType<B>(r3); //fallback for missing C? this is stupid?? can't fallback deeper than field datatype...

            Assert.Equal(0x01, r1.X);
            Assert.Equal(0x02, r2.X);
            Assert.Equal(0x02, r3.X);
        }

        public interface IMarsh1<out T>
        {
            T Read();
        }
        public interface IMarsh11<in T>
            where T : class
        {
            void ReadInto(T current);
        }
        public interface IMarsh2<out T1, in T2>
        {
            T1 Read(T2 current);
        }
        public interface IMarsh3<in T>
        {
            void Read(T current);
        }



        [Fact]
        public void BasicReadTest()
        {


            //register Read marshalers for their implementation types
            //activate value through Activation Marshalers
            //traverse type hierarchy to find first matching Read marshaler
            //pass activated value as current - necessary for Object marshaling, but not primitives
            //passing parent will fuck up variances? :(

            //Expected behaviors
            //- more specific ReadMarshaler can take over BaseMarshaler, eg XBF over U8File
            //- extended custom object with no exact Marshaler can be handled by one matching its ancestor type
            //- object marshaling is to be done on activated object

            //known generic types
            //fieldmarshaling context -> field datatype
            //activation context -> exact value datatype

            //calling Activate from Read
            // - field type is known
            // - activator can return child object
            // - Reader would need to find actual Reader
            // - issue with passing Current remains

            //issue is caused by forcing primitives and complex types into single interface!!!!
            // - primitives do NOT need Activator, thus can be Read<out T>
            // - complex types do NOT need Return value, instead they can mutate input object Read<in T>
            //separating complex/primitive by type is a fucking mess
            //TryResolve<IImmutableRead> with Read<out T> fetched if NO Activator found
            //TryResolve<IMutableRead> with Read<in T> fetched by actual type if Activation succeded
            //this will allow for usecase of deserializing object with no Activation, thus Activation will be optional!
            //but IMutableRead for non reference types would be weeeird, cant ref

            //generic out can be downcasted
            //b1 outputs B, which matches a1 conctract (but outputing A doesnt match contract for outputing B)
            IMarsh1<A> a1 = null;
            IMarsh1<B> b1 = null;
            a1 = b1;
            //b1 = a1;

            IMarsh11<A> a11 = null;
            IMarsh11<B> b11 = null;
            //a11 = b11;
            b11 = a11;

            //in out, b2 can accept B, and outputs more than A
            IMarsh2<A, B> a2 = null;
            IMarsh2<B, A> b2 = null;
            a2 = b2;
            //b2 = a2;

            //a3 can accept B as input, thus in works (but b2 can't work with A input)
            IMarsh3<A> a3 = null;
            IMarsh3<B> b3 = null;
            //a3 = b3;
            b3 = a3;
        }
    }
}
