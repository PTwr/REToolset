using BinaryFile.Unpacker.New.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Tests.New
{
    public class PrimitiveObjectFieldsTests
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

        [Fact]
        public void LambdaDeserialization()
        {
            var store = new MarshalerStore();

            store.RegisterPrimitiveMarshaler(new IntegerMarshaler());

            var mapA = new TypeMarshaler<A, A>();
            store.RegisterRootMap(mapA);

            var mapB = store.GetMarshalerToDerriveFrom<A>()!.Derrive<B>();
            var mapC = store.GetMarshalerToDerriveFrom<B>()!.Derrive<C>();

            var x = new OrderedUnaryFieldMarshaler<A, byte, byte>("X")
                .AtOffset(0).WithOrderOf(1)
                //TODO helper on OrderedUnaryFieldMarshaler when TFieldType == TMarshalingType
                .MarshalInto((o, i, m) => m)
                .Into((A, x) => A.X = x);
            var y = new OrderedUnaryFieldMarshaler<B, byte, byte>("Y")
                .AtOffset(1).WithOrderOf(2)
                .MarshalInto((o, i, m) => m)
                .Into((A, x) => A.Y = x);
            var z = new OrderedUnaryFieldMarshaler<C, byte, byte>("Z")
                .AtOffset(2).WithOrderOf(3)
                .MarshalInto((o, i, m) => m)
                .Into((A, x) => A.Z = x);
            var x2 = new OrderedUnaryFieldMarshaler<C, byte, byte>("X")
                .AtOffset(0).WithOrderOf(1)
                .MarshalInto((o, i, m) => m)
                .Into((A, x) => A.X = x * 10);

            mapA.WithDeserializingAction(x);
            mapB.WithDeserializingAction(y);
            mapC.WithDeserializingAction(z);
            //override X
            mapC.WithDeserializingAction(x2);

            var a = store.GetActivatorFor<A>(null, null).Activate(null, null, null);
            var b = store.GetActivatorFor<B>(null, null).Activate(null, null, a);
            var c = store.GetActivatorFor<C>(null, null).Activate(null, null, b);

            byte[] bytes = [
                1, //x
                2, //y
                3, //z
                ];

            //TODO RootCtx wrapper
            var rootCtx = new MarshalingContext("root", store, null, 0, Unpacker.Metadata.OffsetRelation.Absolute);

            store.GetDeserializatorFor<A>().DeserializeInto(a, bytes.AsSpan(), rootCtx, out _);
            store.GetDeserializatorFor<B>().DeserializeInto(b, bytes.AsSpan(), rootCtx, out _);
            store.GetDeserializatorFor<C>().DeserializeInto(c, bytes.AsSpan(), rootCtx, out _);

            Assert.Equal(1, a.X);

            Assert.Equal(1, b.X);
            Assert.Equal(2, b.Y);

            Assert.Equal(10, c.X);
            Assert.Equal(2, c.Y);
            Assert.Equal(3, c.Z);
        }
    }
}
