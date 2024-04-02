using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryFile.Unpacker.Marshalers;

namespace BinaryFile.Tests.Deserialization
{
    public class StringDeserializationTests
    {
        class POCOWithStrings
        {
            public byte Alength { get; set; }
            public string? A { get; set; }
            public string? B { get; set; }
            public string? C { get; set; }
        }

        [Fact]
        public void BasicStringDeserializationTest()
        {
            var bytes = new byte[] {
                3, 0x41, 0x42, 0x43, //3 ABC
                0x5A, 0x5A, 0x5A, 0x5A, //filler ZZZZ
                0x61, 0x62, 0x00, 0x64, //ab0d
                0x5A, 0x5A, 0x5A, 0x5A, //filler ZZZZ
                0x41, 0x42, 0x43, 0x44, //ABCD
                0x45, 0x46, 0x47, 0x48, //EFGH
            };

            var mgr = new MarshalerManager();
            var ctx = new RootMarshalingContext(mgr, mgr);

            var fluentDeserializer = new FluentMarshaler<POCOWithStrings>();

            fluentDeserializer
                .WithField<byte>("Alength")
                .AtOffset(0)
                .Into((poco, b) => poco.Alength = b);
            fluentDeserializer
                .WithField<string>("A")
                .AtOffset(1)
                .WithLengthOf(poco => poco.Alength)
                .Into((poco, s) => poco.A = s);
            fluentDeserializer
                .WithField<string>("B")
                .AtOffset(8)
                .WithNullTerminator()
                .Into((poco, s) => poco.B = s);
            fluentDeserializer
                .WithField<string>("C")
                .AtOffset(16)
                .WithLengthOf(6)
                .Into((poco, s) => poco.C = s);

            ctx.DeserializerManager.Register(fluentDeserializer);
            ctx.DeserializerManager.Register(new IntegerMarshaler());
            ctx.DeserializerManager.Register(new StringMarshaler());

            var result = fluentDeserializer.Deserialize(bytes, ctx, out var consumedLength);
            Assert.NotNull(result);

            Assert.Equal(3, result.Alength);
            Assert.Equal("ABC", result.A);
            Assert.Equal("ab", result.B);
            Assert.Equal("ABCDEF", result.C);
        }
    }
}
