using BinaryFile.Unpacker.Deserializers;
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

            var deserializer = new ByteArrayDeserializer();

            Assert.True(deserializer.TryDeserialize(source, out var result));
            Assert.NotNull(result);
            Assert.Equal(source, result);
        }

        [Fact]
        public void ByteDeserializer()
        {
            var source = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var deserializer = new ByteDeserializer();

            Assert.True(deserializer.TryDeserialize(source, out var result));
            Assert.Equal(1, result);
        }

        [Fact]
        public void SByteDeserializer()
        {
            var source = new byte[] { 255, 2, 3, 4, 5, 6, 7, 8, 9 };

            var deserializer = new SByteDeserializer();

            Assert.True(deserializer.TryDeserialize(source, out var result));
            Assert.Equal(-1, result);
        }

        [Fact]
        public void UShortDeserializer()
        {
            var source = new byte[] { 0xFF, 0xFF, 3, 4, 5, 6, 7, 8, 9 };

            var deserializer = new UInt16Deserializer();

            Assert.True(deserializer.TryDeserialize(source, out var result));
            Assert.Equal(65535, result);
        }

        [Fact]
        public void ShortDeserializer()
        {
            //TODO add LittleEndian mode!! This is BigEndian inhuman byte order
            var source = new byte[] { 0xC7, 0xCF, 3, 4, 5, 6, 7, 8, 9 };

            var deserializer = new Int16Deserializer();

            Assert.True(deserializer.TryDeserialize(source, out var result));
            Assert.Equal(-12345, result);
        }

        //TODO other deserializers
    }
}
