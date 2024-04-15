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
    public class NestedObjectsTests
    {
        class A
        {
            public byte X { get; set; }
            public B B { get; set; }
        }
        class B
        {
            public byte Y { get; set; }
            public C C { get; set; }
        }
        class C
        {
            public byte Z { get; set; }
        }

        byte[] data = [
            1,2,3,
            ];

        [Fact]
        public void SimpleDeserializeSerializeLoop()
        {
            var store = new MarshalerStore();

            store.RegisterPrimitiveMarshaler(new IntegerMarshaler());

            var mapA = new TypeMarshaler<A, A>();
            var mapB = new TypeMarshaler<B, B>();
            var mapC = new TypeMarshaler<C, C>();
            store.RegisterRootMap(mapA);
            store.RegisterRootMap(mapB);
            store.RegisterRootMap(mapC);

            var x = new OrderedUnaryFieldMarshaler<A, byte, byte>("X")
                .AtOffset(0).WithOrderOf(1)
                .MarshalInto((o, i, m) => m)
                .MarshalFrom((o, i) => i)
                .Into((A, x) => A.X = x)
                .From((A) => A.X);
            var y = new OrderedUnaryFieldMarshaler<B, byte, byte>("Y")
                .AtOffset(0).WithOrderOf(1)
                .MarshalInto((o, i, m) => m)
                .MarshalFrom((o, i) => i)
                .Into((A, x) => A.Y = x)
                .From((A) => A.Y);
            var z = new OrderedUnaryFieldMarshaler<C, byte, byte>("Z")
                .AtOffset(0).WithOrderOf(1)
                .MarshalInto((o, i, m) => m)
                .MarshalFrom((o, i) => i)
                .Into((A, x) => A.Z = x)
                .From((A) => A.Z);

            var a = new OrderedUnaryFieldMarshaler<A, B, B>("B")
                .AtOffset(1).WithOrderOf(1)
                .MarshalInto((o, i, m) => m)
                .MarshalFrom((o, i) => i)
                .Into((A, x) => A.B = x)
                .From((A) => A.B);
            var b = new OrderedUnaryFieldMarshaler<B, C, C>("C")
                .AtOffset(1).WithOrderOf(1)
                .MarshalInto((o, i, m) => m)
                .MarshalFrom((o, i) => i)
                .Into((A, x) => A.C = x)
                .From((A) => A.C);

            mapA.WithMarshalingAction(x);
            mapA.WithMarshalingAction(a);

            mapB.WithMarshalingAction(y);
            mapB.WithMarshalingAction(b);

            mapC.WithMarshalingAction(z);

            var rootCtx = new MarshalingContext("root", store, null, 0, OffsetRelation.Absolute, null);
            ByteBuffer bb = new ByteBuffer();

            var A = mapA.DeserializeInto(new A(), data.AsSpan(), rootCtx, out _);

            Assert.Equal(1, A.X);
            Assert.NotNull(A.B);
            Assert.Equal(2, A.B.Y);
            Assert.NotNull(A.B.C);
            Assert.Equal(3, A.B.C.Z);

            mapA.SerializeFrom(A, bb, rootCtx, out _);
            var newData = bb.GetData();

            Assert.Equal(data, newData);
        }
    }
}
