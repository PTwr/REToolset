using BinaryFile.Unpacker.Deserializers;
using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Tests
{
    public class FluentDeserializerTests
    {
        class FlatPOCO
        {
            public byte A { get; set; }
            public byte B { get; set; }
            public byte C { get; set; }
            public byte D { get; set; }
        }
        [Fact]
        public void FlatPOCOTest()
        {
            var bytes = new byte[] {
                1, 2, 3, 4,
            };

            var ctx = new RootDataOffset(new DeserializerManager());

            FluentDeserializer<FlatPOCO> fluentDeserializer = PrepareFlatPOCODeserializer();

            ctx.Manager.Register(fluentDeserializer);
            ctx.Manager.Register(new IntegerDeserializer());

            var result = fluentDeserializer.Deserialize(bytes, out var success, ctx, out var consumedLength);
            Assert.NotNull(result);
            Assert.True(success);

            Assert.Equal(1, result.A);
            Assert.Equal(2, result.B);
            Assert.Equal(3, result.C);
            Assert.Equal(4, result.D);
        }

        private static FluentDeserializer<FlatPOCO> PrepareFlatPOCODeserializer()
        {
            var fluentDeserializer = new FluentDeserializer<FlatPOCO>();

            //TODO setter from getter in first invocation
            //TODO add Expression overloads to get nice ToString description. Will require getter->setter transformation as embedeed Expresssion can't contain assignment as of yet.
            fluentDeserializer
                .WithField<byte>("A")
                .AtOffset(0)
                .Into((poco, b) => poco.A = b);
            fluentDeserializer
                .WithField<byte>("B")
                .AtOffset(1)
                .Into((poco, b) => poco.B = b);
            fluentDeserializer
                .WithField<byte>("C")
                .AtOffset(2)
                .Into((poco, b) => poco.C = b);
            fluentDeserializer
                .WithField<byte>("D")
                .AtOffset(3)
                .Into((poco, b) => poco.D = b);
            return fluentDeserializer;
        }

        class POCOWithChildren
        {
            public byte A { get; set; }
            public byte B { get; set; }

            public FlatPOCO ChildA { get; set; }

            public byte C { get; set; }
            public byte D { get; set; }

            public FlatPOCO ChildB { get; set; }

        }
        [Fact]
        public void POCOWithChildrenTest()
        {
            var bytes = new byte[] {
                1, 2, //A B
                11, 12, 13, 14, //ChildA ABCD
                3, 4, //C D
                21, 22, 23, 24, //ChildA ABCD
            };

            var ctx = new RootDataOffset(new DeserializerManager());

            FluentDeserializer<FlatPOCO> flatPOCODeserializer = PrepareFlatPOCODeserializer();

            var POCOWithChildrenDeserializer = new FluentDeserializer<POCOWithChildren>();
            POCOWithChildrenDeserializer
                .WithField<byte>("A")
                .AtOffset(0)
                .Into((poco, b) => poco.A = b);
            POCOWithChildrenDeserializer
                .WithField<byte>("B")
                .AtOffset(1)
                .Into((poco, b) => poco.B = b);
            POCOWithChildrenDeserializer
                .WithField<byte>("C")
                .AtOffset(6)
                .Into((poco, b) => poco.C = b);
            POCOWithChildrenDeserializer
                .WithField<byte>("D")
                .AtOffset(7)
                .Into((poco, b) => poco.D = b);

            POCOWithChildrenDeserializer
                .WithField<FlatPOCO>("ChildA")
                .AtOffset(2)
                .Into((obj, val) => obj.ChildA = val);
            POCOWithChildrenDeserializer
                .WithField<FlatPOCO>("ChildB")
                .AtOffset(8)
                .Into((obj, val) => obj.ChildB = val);


            ctx.Manager.Register(flatPOCODeserializer);
            ctx.Manager.Register(new IntegerDeserializer());

            var result = POCOWithChildrenDeserializer.Deserialize(bytes, out var success, ctx, out var consumedLength);
            Assert.NotNull(result);
            Assert.True(success);

            Assert.Equal(1, result.A);
            Assert.Equal(2, result.B);
            Assert.Equal(3, result.C);
            Assert.Equal(4, result.D);

            Assert.Equal(11, result.ChildA.A);
            Assert.Equal(12, result.ChildA.B);
            Assert.Equal(13, result.ChildA.C);
            Assert.Equal(14, result.ChildA.D);

            Assert.Equal(21, result.ChildB.A);
            Assert.Equal(22, result.ChildB.B);
            Assert.Equal(23, result.ChildB.C);
            Assert.Equal(24, result.ChildB.D);
        }
    }
}
