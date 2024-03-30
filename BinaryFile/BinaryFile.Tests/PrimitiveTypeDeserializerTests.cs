using BinaryFile.Unpacker;
using BinaryFile.Unpacker.Deserializers;
using BinaryFile.Unpacker.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Tests
{
    public class PrimitiveTypeDeserializerTests
    {
        [Fact]
        public void ByteArrayDeserializer()
        {
            var source = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            IDeserializer<byte[]> deserializer = new BinaryArrayDeserializer();

            var result = deserializer.Deserialize(source, out var success, new RootDataOffset(null));
            Assert.True(success);
            Assert.NotNull(result);
            Assert.Equal(source, result);
        }

        [Fact]
        public void ByteDeserializer()
        {
            var source = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            IDeserializer<byte> deserializer = new IntegerDeserializer();

            var result = deserializer.Deserialize(source, out var success, new RootDataOffset(null));
            Assert.True(success);

            Assert.Equal(1, result);
        }

        //TODO other deserializers
    }
}
