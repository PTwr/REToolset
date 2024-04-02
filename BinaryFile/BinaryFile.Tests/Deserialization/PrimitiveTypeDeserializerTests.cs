using BinaryFile.Unpacker;
using BinaryFile.Unpacker.Marshalers;
using BinaryFile.Unpacker.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Tests.Deserialization
{
    public class PrimitiveTypeDeserializerTests
    {
        class EndianTestPOCO
        {
            public int A { get; set; }
            public int B { get; set; }
        }

        [Fact]
        public void EndiannesTest()
        {
            var bytes = new byte[]
            {
                0xD2, 0x02, 0x96, 0x49, //1234567890 little endian
                0x49, 0x96, 0x02, 0xD2, //1234567890 big endian
            };

            var deserializer = new FluentMarshaler<EndianTestPOCO>();
            deserializer.WithField<int>("A").AtOffset(0).InLittleEndian(inLittleEndian: true).Into((i, x) => i.A = x);
            //TODO add BigEndina method?
            deserializer.WithField<int>("A").AtOffset(4).InLittleEndian(inLittleEndian: false).Into((i, x) => i.B = x);

            var mgr = new MarshalerManager();
            var ctx = new RootMarshalingContext(mgr, mgr);
            ctx.DeserializerManager.Register(new IntegerMarshaler());
            ctx.DeserializerManager.Register(deserializer);

            var result = deserializer.Deserialize(bytes.AsSpan(), ctx, out _);

            Assert.Equal(1234567890, result.A);
            Assert.Equal(1234567890, result.B);
        }


        [Fact]
        public void ByteArrayDeserializer()
        {
            var source = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            IDeserializer<byte[]> deserializer = new BinaryArrayMarshaler();

            var mgr = new MarshalerManager();
            var ctx = new RootMarshalingContext(mgr, mgr);
            var result = deserializer.Deserialize(source, ctx, out _);
            Assert.NotNull(result);
            Assert.Equal(source, result);
        }

        [Fact]
        public void ByteDeserializer()
        {
            var source = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            IDeserializer<byte> deserializer = new IntegerMarshaler();

            var mgr = new MarshalerManager();
            var ctx = new RootMarshalingContext(mgr, mgr);
            var result = deserializer.Deserialize(source, ctx, out _);

            Assert.Equal(1, result);
        }

        //TODO other basic deserializers
    }
}
