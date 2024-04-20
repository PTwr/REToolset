using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.PrimitiveMarshaling;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.Tests
{
    public class DeserializationTests
    {
        class SingleLevel
        {
            public byte A { get; set; }
            public byte B { get; set; }
            public byte C { get; set; }
            public byte D { get; set; }
        }

        [Fact]
        public void SingleLevelDeserialization()
        {
            IMarshalerStore store = new MarshalerStore();
            var map = new TypeMarshaler<SingleLevel, SingleLevel, SingleLevel>();
            store.Register(map);
            var intMap = new IntegerMarshaler();
            store.Register(intMap);

            map.WithField(i => i.A).AtOffset(0);
            map.WithField(i => i.B).AtOffset(1);
            map.WithField(i => i.C).AtOffset(2);
            map.WithField(i => i.D).AtOffset(3);

            byte[] bytes = [1, 2, 3, 4];

            var d = store.FindMarshaler<SingleLevel>();

            var rootCtx = new MarshalingContext("root", store, null, 0, Common.OffsetRelation.Absolute, null);
            var x = d.Deserialize(null, null, bytes.AsMemory(), rootCtx, out _);

            Assert.IsType<SingleLevel>(x);

            Assert.Equal(1, x.A);
            Assert.Equal(2, x.B);
            Assert.Equal(3, x.C);
            Assert.Equal(4, x.D);
        }
    }
}
