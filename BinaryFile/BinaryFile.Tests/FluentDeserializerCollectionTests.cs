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
    public class FluentDeserializerCollectionTests
    {
        class RootObjA
        {
            public byte A { get; set; }
            public byte B { get; set; }

            public List<ChildObjA>? Children { get; set; }
            public class ChildObjA
            {
                public byte A { get; set; }
                public byte B { get; set; }
            }
        }

        [Fact]
        public void SimpleCollectionTest()
        {
            var bytes = new byte[] {
                1, 2, 
                3, 4,
                5, 6,
                7, 8,
                9, 0,
            };

            var ctx = new RootDataOffset(new DeserializerManager());

            FluentDeserializer<RootObjA> fluentDeserializer = new FluentDeserializer<RootObjA>();
            FluentDeserializer<RootObjA.ChildObjA> childDeserializer = new FluentDeserializer<RootObjA.ChildObjA>();

            childDeserializer
                .WithField<byte>("ChildA")
                .AtOffset(0)
                .Into((poco, b) => poco.A = b);
            childDeserializer
                .WithField<byte>("ChildB")
                .AtOffset(1)
                .Into((poco, b) => poco.B = b);

            fluentDeserializer
                .WithField<byte>("A")
                .AtOffset(0)
                .Into((poco, b) => poco.A = b);
            fluentDeserializer
                .WithField<byte>("B")
                .AtOffset(1)
                .Into((poco, b) => poco.B = b);

            fluentDeserializer
                .WithCollectionOf<RootObjA.ChildObjA>("Children")
                .AtOffset(2)
                .WithCountOf(4)
                .WithItemLengthOf(2)
                .Into((poco, items) => poco.Children = items.ToList());

            ctx.Manager.Register(childDeserializer);
            ctx.Manager.Register(fluentDeserializer);
            ctx.Manager.Register(new IntegerDeserializer());

            var result = fluentDeserializer.Deserialize(bytes, ctx, out var consumedLength);
            Assert.NotNull(result);

            Assert.Equal(1, result.A);
            Assert.Equal(2, result.B);

            Assert.NotNull(result.Children);
            Assert.Equal(4, result.Children.Count);

            Assert.Equal(3, result.Children[0].A);
            Assert.Equal(4, result.Children[0].B);

            Assert.Equal(5, result.Children[1].A);
            Assert.Equal(6, result.Children[1].B);

            Assert.Equal(7, result.Children[2].A);
            Assert.Equal(8, result.Children[2].B);

            Assert.Equal(9, result.Children[3].A);
            Assert.Equal(0, result.Children[3].B);
        }
    }
}
