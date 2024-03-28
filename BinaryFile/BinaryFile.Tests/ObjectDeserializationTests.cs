using BinaryFile.Unpacker.Deserializers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Tests
{
    public class ObjectDeserializationTests
    {
        class POCO
        {
            public byte Foo { get; set; }
            public ushort Bar { get; set; }
        }

        [Fact]        
        public void SingleLevelPOCOTest()
        {
            var bytes = new byte[] { 0xFF, 0xFF, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };

            var dd = new ObjectDeserializer<POCO>();

            //TODO field position
            dd.Field(poco => poco.Foo);
            dd.Field(poco => poco.Bar);

            Assert.True(dd.TryDeserialize(bytes, out var result));
            Assert.NotNull(result);

            Assert.Equal(0xFF, result.Foo);
            Assert.Equal(0xFFFF, result.Bar);
        }
    }
}
