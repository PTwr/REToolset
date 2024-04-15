using BinaryDataHelper;
using BinaryFile.Unpacker.New.Implementation;
using BinaryFile.Unpacker.New.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Tests.New
{
    public class NewTypeMarshalerLambdaTests
    {
        class A
        {
            public int X { get; set; }
        }
        class B : A
        {
            public A parentA;
            public B()
            {

            }
            public B(A parent)
            {
                this.parentA = parent;
            }
            public int Y { get; set; }
        }
        class C : B
        {
            public B parentB;
            public C()
            {

            }
            public C(B parent) : base(parent)
            {
                this.parentB = parent;
            }
            public int Z { get; set; }
        }

        //TODO this might be useful in real maps insted of empty .Into() clauses mixed with events. Would allow to invoke custom code in-between (de)serializing fields
        class LambdaFieldMarshaler<TMappedType, TFieldType>
            : OrderedFieldMarshaler<TMappedType, TFieldType, TFieldType, LambdaFieldMarshaler<TMappedType, TFieldType>>
            where TMappedType : class
        {
            private readonly Action<TMappedType>? deserialize;
            private readonly Action<TMappedType>? serialize;

            public LambdaFieldMarshaler(string name, Action<TMappedType>? deserialize = null, Action<TMappedType>? serialize = null) : base(name)
            {
                this.deserialize = deserialize;
                this.serialize = serialize;

                IsDeserializationEnabled = deserialize is not null;
                IsSerializationEnabled = serialize is not null;
            }

            public override void DeserializeInto(TMappedType mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh)
            {
                deserialize(mappedObject);
                fieldByteLengh = 0;
            }

            public override void SerializeFrom(TMappedType mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh)
            {
                serialize(mappedObject);
                fieldByteLengh = 0;
            }
        }

        [Fact]
        public void HierarchicalRegistration()
        {
            var store = new MarshalerStore();

            var rootMap = new TypeMarshaler<A, A>();

            store.RegisterRootMap(rootMap);

            var shouldBeRootMap = store.GetMarshalerToDerriveFrom<A>();

            Assert.NotNull(shouldBeRootMap);
            Assert.Equal(rootMap, shouldBeRootMap);

            var mapB = shouldBeRootMap.Derrive<B>();

            var shouldBeMapB = store.GetObjectDeserializerFor<B>();
            var thisToShouldBeMapB = store.GetMarshalerToDerriveFrom<B>();

            Assert.NotNull(shouldBeMapB);
            Assert.NotNull(thisToShouldBeMapB);
            Assert.Equal(mapB, shouldBeMapB);
            Assert.Equal(mapB, thisToShouldBeMapB);

            var mapC = thisToShouldBeMapB.Derrive<C>();

            var shouldBeMapC = store.GetObjectDeserializerFor<C>();

            Assert.NotNull(shouldBeMapC);
            Assert.Equal(mapC, shouldBeMapC);
        }

        [Fact]
        public void HierarchicalActivation()
        {
            var store = new MarshalerStore();

            var rootMap = new TypeMarshaler<A, A>();
            store.RegisterRootMap(rootMap);

            //HierarchicalRegistration does tests for this
            store.GetMarshalerToDerriveFrom<A>()!.Derrive<B>();
            store.GetMarshalerToDerriveFrom<B>()!.Derrive<C>();

            var activatorForA = store.GetActivatorFor<A>(null, null);
            var activatorForB = store.GetActivatorFor<B>(null, null);
            var activatorForC = store.GetActivatorFor<C>(null, null);

            Assert.NotNull(activatorForA);
            Assert.NotNull(activatorForB);
            Assert.NotNull(activatorForC);

            var a = activatorForA.Activate(null, null, null);
            var b = activatorForB.Activate(null, null, null);
            var c = activatorForC.Activate(null, null, null);

            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.NotNull(c);

            a = activatorForA.Activate(null, null, null);
            b = activatorForB.Activate(null, null, a);
            c = activatorForC.Activate(null, null, b);

            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.NotNull(c);

            Assert.Equal(a, b.parentA);
            Assert.Equal(b, c.parentB);
        }

        [Fact]
        public void LambdaDeserialization()
        {
            var store = new MarshalerStore();

            var mapA = new TypeMarshaler<A, A>();
            store.RegisterRootMap(mapA);

            var mapB = store.GetMarshalerToDerriveFrom<A>()!.Derrive<B>();
            var mapC = store.GetMarshalerToDerriveFrom<B>()!.Derrive<C>();

            mapA.WithMarshalingAction(new LambdaFieldMarshaler<A, int>("X", i => i.X = 2).WithOrderOf(0));
            mapB.WithMarshalingAction(new LambdaFieldMarshaler<B, int>("Y", i => i.Y = i.X * 2).WithOrderOf(1));
            mapC.WithMarshalingAction(new LambdaFieldMarshaler<C, int>("Z", i => i.Z = i.Y * 2).WithOrderOf(2));
            //override X
            mapC.WithMarshalingAction(new LambdaFieldMarshaler<C, int>("X", i => i.X = 3).WithOrderOf(-1));

            var a = store.GetActivatorFor<A>(null, null).Activate(null, null, null);
            var b = store.GetActivatorFor<B>(null, null).Activate(null, null, a);
            var c = store.GetActivatorFor<C>(null, null).Activate(null, null, b);

            store.GetDeserializatorFor<A>().DeserializeInto(a, null, null, out _);
            store.GetDeserializatorFor<B>().DeserializeInto(b, null, null, out _);
            store.GetDeserializatorFor<C>().DeserializeInto(c, null, null, out _);

            Assert.Equal(2, a.X);

            Assert.Equal(2, b.X);
            Assert.Equal(4, b.Y);

            Assert.Equal(3, c.X);
            Assert.Equal(6, c.Y);
            Assert.Equal(12, c.Z);
        }

        [Fact]
        public void LambdaSerialization()
        {
            var a = new A() { X = 123, };
            var b = new B(a) { X = a.X, Y = 456, };
            var c = new C(b) { X = 1, Y = 2, Z = 3 };

            var store = new MarshalerStore();

            var mapA = new TypeMarshaler<A, A>();
            store.RegisterRootMap(mapA);

            var mapB = store.GetMarshalerToDerriveFrom<A>()!.Derrive<B>();
            var mapC = store.GetMarshalerToDerriveFrom<B>()!.Derrive<C>();

            int fakeOutputX = 0;
            int fakeOutputY = 0;
            int fakeOutputZ = 0;
            mapA.WithMarshalingAction(new LambdaFieldMarshaler<A, int>("X", null, i => fakeOutputX = i.X).WithOrderOf(0));
            mapB.WithMarshalingAction(new LambdaFieldMarshaler<B, int>("Y", null, i => fakeOutputY = i.Y).WithOrderOf(1));
            mapC.WithMarshalingAction(new LambdaFieldMarshaler<C, int>("Z", null, i => fakeOutputZ = i.Z).WithOrderOf(2));
            //overrides
            mapC.WithMarshalingAction(new LambdaFieldMarshaler<C, int>("X", null, i => fakeOutputX = i.X * 10).WithOrderOf(-1));
            mapC.WithMarshalingAction(new LambdaFieldMarshaler<C, int>("Z", null, i => fakeOutputZ = fakeOutputX * 10).WithOrderOf(100));

            store.GetObjectSerializerFor<A>().SerializeFrom(a, null, null, out _);
            Assert.Equal(123, fakeOutputX);

            store.GetObjectSerializerFor<B>().SerializeFrom(b, null, null, out _);
            Assert.Equal(123, fakeOutputX);
            Assert.Equal(456, fakeOutputY);

            store.GetObjectSerializerFor<C>().SerializeFrom(c, null, null, out _);
            Assert.Equal(10, fakeOutputX);
            Assert.Equal(2, fakeOutputY);
            Assert.Equal(100, fakeOutputZ);
        }
    }
}
