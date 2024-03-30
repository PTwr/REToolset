using BinaryFile.Unpacker;
using BinaryFile.Unpacker.Deserializers;
using BinaryFile.Unpacker.Metadata;
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
            public byte A { get; set; }
            public byte B { get; set; }
            public byte C { get; set; }
            public byte D { get; set; }
        }

        [Fact]        
        public void SingleLevelPOCOTest()
        {
            var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };

            var ctx = new RootDataOffset(new DeserializerManager());

            var dd = new ObjectDeserializer<POCO>();

            ctx.Manager.Register(dd);
            ctx.Manager.Register(new IntegerDeserializer());

            //TODO field position
            dd.Field(poco => poco.A).AtOffset(OffsetRelation.Absolute, 0);
            dd.Field(poco => poco.B).AtOffset(OffsetRelation.Absolute, 1);
            dd.Field(poco => poco.C).AtOffset(OffsetRelation.Absolute, 2);
            dd.Field(poco => poco.D).AtOffset(OffsetRelation.Absolute, 3);

            var result = dd.Deserialize(bytes, out var success, ctx);
            Assert.NotNull(result);

            Assert.Equal(0, result.A);
            Assert.Equal(1, result.B);
            Assert.Equal(2, result.C);
            Assert.Equal(3, result.D);
        }
    }
}
