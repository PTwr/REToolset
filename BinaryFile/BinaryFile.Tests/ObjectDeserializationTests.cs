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
            public byte Abis { get; set; }
            public byte B { get; set; }
            public byte C { get; set; }
            public byte D { get; set; }

            public int E { get; set; }
            public int F { get; set; }

            public NestedPOCO NestedPOCO { get; set; }
        }

        class NestedPOCO
        {
            public ushort A { get; set; }
            public bool B { get; set; }
            public sbyte C { get; set; }
            public sbyte Cbis { get; set; }
            public sbyte Cter { get; set; }
        }


        [Fact]
        public void SingleLevelPOCOTest()
        {
            var bytes = new byte[] {
                1, 2, 3, 4,
                0xD2, 0x02, 0x96, 0x49, //1234567890, Damn Intel and its Big Endian!!!
                0x2E, 0xFD, 0x69, 0xB6, // -1234567890 big endian
            };

            var ctx = new RootDataOffset(new DeserializerManager());

            var dd = new ObjectDeserializer<POCO>();

            ctx.Manager.Register(dd);
            ctx.Manager.Register(new IntegerDeserializer());

            //Segment and Absolute should behave 
            dd.Field(poco => poco.A).AtOffset(OffsetRelation.Absolute, 0);
            dd.Field(poco => poco.Abis).AtOffset(OffsetRelation.Segment, 0);

            dd.Field(poco => poco.B).AtOffset(OffsetRelation.Absolute, 1);
            dd.Field(poco => poco.C).AtOffset(OffsetRelation.Absolute, 2);
            dd.Field(poco => poco.D).AtOffset(OffsetRelation.Absolute, 3);

            dd.Field(poco => poco.E).AtOffset(OffsetRelation.Absolute, 4);
            dd.Field(poco => poco.F).AtOffset(OffsetRelation.Absolute, 8);

            var result = dd.Deserialize(bytes, out var success, ctx);
            Assert.NotNull(result);
            Assert.True(success);

            Assert.Equal(1, result.A);
            Assert.Equal(1, result.Abis);
            Assert.Equal(2, result.B);
            Assert.Equal(3, result.C);
            Assert.Equal(4, result.D);

            Assert.Equal(1234567890, result.E);
            Assert.Equal(-1234567890, result.F);
        }

        [Fact]
        public void NestedPOCOTest()
        {
            var bytes = new byte[] {
                1, 2, 3, 4,
                0xD2, 0x02, 0x96, 0x49, //1234567890, Damn Intel and its Big Endian!!!
                0x2E, 0xFD, 0x69, 0xB6, // -1234567890 big endian
                0xFF, 0xFF, 1, 0x85, //nested
            };

            var ctx = new RootDataOffset(new DeserializerManager());

            var dd = new ObjectDeserializer<POCO>();
            var nested = new ObjectDeserializer<NestedPOCO>();

            ctx.Manager.Register(dd);
            ctx.Manager.Register(nested);
            ctx.Manager.Register(new IntegerDeserializer());

            dd.Field(poco => poco.A).AtOffset(OffsetRelation.Absolute, 0);
            dd.Field(poco => poco.B).AtOffset(OffsetRelation.Absolute, 1);

            dd.Field(poco => poco.NestedPOCO).AtOffset(OffsetRelation.Segment, 12);

            nested.Field(nested => nested.A).AtOffset(OffsetRelation.Segment, 0);
            nested.Field(nested => nested.B).AtOffset(OffsetRelation.Segment, 2);

            //should behave the same
            nested.Field(nested => nested.C).AtOffset(OffsetRelation.Segment, 3);
            nested.Field(nested => nested.Cbis).AtOffset(OffsetRelation.Parent, 15);
            nested.Field(nested => nested.Cter).AtOffset(OffsetRelation.Absolute, 15);

            var result = dd.Deserialize(bytes, out var success, ctx);
            Assert.NotNull(result);
            Assert.True(success);

            //ensure nested objects don't fuck up simple fields
            Assert.Equal(1, result.A);
            Assert.Equal(2, result.B);

            Assert.NotNull(result.NestedPOCO);

            Assert.Equal(65535, result.NestedPOCO.A);
            Assert.True(result.NestedPOCO.B);
            Assert.Equal(-123, result.NestedPOCO.C);
            Assert.Equal(-123, result.NestedPOCO.Cbis);
            Assert.Equal(-123, result.NestedPOCO.Cter);
        }

        class CollectionOfPOCO
        {
            public byte A { get; set; }
            public byte B { get; set; }
            public IList<POCOChild>? ChildrenA { get; set; }
            public POCOChild[]? ChildrenB { get; set; }

            public class POCOChild
            {
                public byte A { get; set; }
                public byte B { get; set; }
            }
        }

        [Fact]
        public void CollectionPOCOTest()
        {
            var bytes = new byte[] {
                1, 2,
                3, 4,
                5, 6,
                7, 8,
                9, 0,
            };

            var ctx = new RootDataOffset(new DeserializerManager());

            var dd = new ObjectDeserializer<CollectionOfPOCO>();
            var ddc = new ObjectDeserializer<CollectionOfPOCO.POCOChild>();

            ctx.Manager.Register(dd);
            ctx.Manager.Register(ddc);
            ctx.Manager.Register(new IntegerDeserializer());

            dd.Field(poco => poco.A).AtOffset(OffsetRelation.Segment, 0);
            dd.Field(poco => poco.B).AtOffset(OffsetRelation.Segment, 1);

            ddc.Field(child => child.A).AtOffset(OffsetRelation.Segment, 0);
            ddc.Field(child => child.B).AtOffset(OffsetRelation.Segment, 1);

            dd.CollectionField(poco => poco.ChildrenA)
                .AtOffset(OffsetRelation.Segment, 2)
                .WithCountOf(2);
            dd.CollectionField(poco => poco.ChildrenB)
                .AtOffset(OffsetRelation.Segment, 6)
                .WithCountOf(2);

            dd.ColField<CollectionOfPOCO.POCOChild[], CollectionOfPOCO.POCOChild>(poco => poco.ChildrenB)
                .AtOffset(OffsetRelation.Segment, 6)
                .WithCountOf(2);

        }
    }
}
