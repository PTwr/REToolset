using BinaryDataHelper;
using BinaryFile.Unpacker.New.Implementation;
using BinaryFile.Unpacker.New.Implementation.ObjectMarshalers;
using BinaryFile.Unpacker.New.Implementation.ObjectMarshalers.FieldMarshalers;
using BinaryFile.Unpacker.New.Implementation.PrimitiveMarshalers;
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

            mapA.WithField("X", a => a.X).AtOffset(0);
            mapA.WithField("B", a => a.B).AtOffset(1);

            mapB.WithField("Y", a => a.Y).AtOffset(0);
            mapB.WithField("C", a => a.C).AtOffset(1);

            mapC.WithField("X", a => a.Z).AtOffset(0);

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
