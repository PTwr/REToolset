﻿using BinaryFile.Unpacker.Deserializers;
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
        public void POCOWithChildrenInerleavedWithParentFieldsTest()
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

        class POCOWithChildLookingOutsideTheirSegment
        {
            public byte A { get; set; }
            public byte B { get; set; }
            public byte C { get; set; }
            public byte D { get; set; }

            public POCOReferecingAbsoluteScope Child { get; set; }

            public class POCOReferecingAbsoluteScope
            {
                public byte A { get; set; }
                public byte B { get; set; }

                public byte Cabsolute { get; set; }
                public byte Dparent { get; set; }

                public POCOReferencingHigherScopesScopes GrandChild { get; set; }

                public class POCOReferencingHigherScopesScopes
                {
                    public byte Asegment { get; set; }
                    public byte Aparent { get; set; }
                    public byte Agrandparent { get; set; }
                    public byte Aabsolute { get; set; }
                }
            }
        }

        [Fact]
        public void ThreeLevelPOCOWithOutOfSegmentFieldOffsetsTest()
        {
            var ctx = new RootDataOffset(new DeserializerManager());

            var grandChildDeserializer = new FluentDeserializer<POCOWithChildLookingOutsideTheirSegment.POCOReferecingAbsoluteScope.POCOReferencingHigherScopesScopes>();

            grandChildDeserializer
                .WithField<byte>("Asegment")
                .AtOffset(0)
                .Into((poco, b) => poco.Asegment = b);
            grandChildDeserializer
                .WithField<byte>("Aparent")
                .AtOffset(0, OffsetRelation.Parent)
                .Into((poco, b) => poco.Aparent = b);
            grandChildDeserializer
                .WithField<byte>("Agrandparent")
                .AtOffset(0, OffsetRelation.GrandParent)
                .Into((poco, b) => poco.Agrandparent = b);
            grandChildDeserializer
                .WithField<byte>("Aabsolute")
                .AtOffset(0, OffsetRelation.Absolute)
                .Into((poco, b) => poco.Aabsolute = b);

            var childDeserializer = new FluentDeserializer<POCOWithChildLookingOutsideTheirSegment.POCOReferecingAbsoluteScope>();

            childDeserializer
                .WithField<byte>("A")
                .AtOffset(0)
                .Into((poco, b) => poco.A = b);
            childDeserializer
                .WithField<byte>("B")
                .AtOffset(1)
                .Into((poco, b) => poco.B = b);

            childDeserializer
                .WithField<byte>("Cabsolute")
                .AtOffset(2, OffsetRelation.Absolute)
                .Into((poco, b) => poco.Cabsolute = b);
            childDeserializer
                .WithField<byte>("Dparent")
                .AtOffset(3, OffsetRelation.Parent)
                .Into((poco, b) => poco.Dparent = b);

            childDeserializer
                .WithField<POCOWithChildLookingOutsideTheirSegment.POCOReferecingAbsoluteScope.POCOReferencingHigherScopesScopes>("GrandChild")
                .AtOffset(2, OffsetRelation.Segment)
                .Into((obj, val) => obj.GrandChild = val);

            var rootDeserializer = new FluentDeserializer<POCOWithChildLookingOutsideTheirSegment>();

            rootDeserializer
                .WithField<byte>("A")
                .AtOffset(0)
                .Into((poco, b) => poco.A = b);
            rootDeserializer
                .WithField<byte>("B")
                .AtOffset(1)
                .Into((poco, b) => poco.B = b);
            rootDeserializer
                .WithField<byte>("C")
                .AtOffset(2)
                .Into((poco, b) => poco.C = b);
            rootDeserializer
                .WithField<byte>("D")
                .AtOffset(3)
                .Into((poco, b) => poco.D = b);

            rootDeserializer
                .WithField<POCOWithChildLookingOutsideTheirSegment.POCOReferecingAbsoluteScope>("Child")
                .AtOffset(4)
                .Into((obj, val) => obj.Child = val);

            ctx.Manager.Register(grandChildDeserializer);
            ctx.Manager.Register(childDeserializer);
            ctx.Manager.Register(rootDeserializer);
            ctx.Manager.Register(new IntegerDeserializer());

            var bytes = new byte[] {
                1, 2, 3, 4, //root A B C D
                11, 12, //Child A B
                31, 32, 33, 34, //GrandChild A
            };

            var result = rootDeserializer.Deserialize(bytes, out var success, ctx, out var consumedLength);
            Assert.NotNull(result);
            Assert.True(success);

            Assert.Equal(1, result.A);
            Assert.Equal(2, result.B);
            Assert.Equal(3, result.C);
            Assert.Equal(4, result.D);
            Assert.NotNull(result.Child);

            Assert.Equal(11, result.Child.A);
            Assert.Equal(12, result.Child.B);
            Assert.Equal(3, result.Child.Cabsolute);
            Assert.Equal(4, result.Child.Dparent);
            Assert.NotNull(result.Child.GrandChild);

            Assert.Equal(31, result.Child.GrandChild.Asegment);
            Assert.Equal(1, result.Child.GrandChild.Aabsolute);
            Assert.Equal(11, result.Child.GrandChild.Aparent);
            Assert.Equal(1, result.Child.GrandChild.Agrandparent);
        }

        class POCOWithPrimitiveArrays
        {
            public byte[] A { get; set; }

            public byte BLength { get; set; }
            public byte[] B { get; set; }

            public byte[] C { get; set; }

            public byte[] D { get; set; }
        }

        [Fact]
        public void PrimitiveArrayFieldDeserializationTest()
        {
            var bytes = new byte[] {
                1, 2, 3, 4, //A[]
                3, 12, 13, 14, //BLength, B[]
                31, 32, 33, 34, //C[]
                41, 42, 43, 44, //D[]
            };

            var deserializer = new FluentDeserializer<POCOWithPrimitiveArrays>();
            deserializer.WithField<byte[]>("Fixed Length A[]").AtOffset(0).WithLengthOf(4).Into((poco, arr) => poco.A = arr);
            deserializer.WithField<byte>("Length of B[]").AtOffset(4).Into((poco, l) => poco.BLength = l);
            deserializer.WithField<byte[]>("Variable length array B[]").AtOffset(5).WithLengthOf(poco => poco.BLength).Into((poco, arr) => poco.B = arr);

            deserializer.WithField<byte[]>("Post processing array C[]").AtOffset(8).WithLengthOf(4).Into((poco, arr) => poco.C = arr.Reverse().ToArray());

            deserializer.WithField<byte[]>("Until end of segment array D[]").AtOffset(12).WithLengthOf(4).Into((poco, arr) => poco.D = arr);

            var ctx = new RootDataOffset(new DeserializerManager());
            ctx.Manager.Register(deserializer);
            ctx.Manager.Register(new BinaryArrayDeserializer());
            ctx.Manager.Register(new IntegerDeserializer());

            var result = deserializer.Deserialize(bytes, out var success, ctx, out var consumedLength);
            Assert.NotNull(result);
            Assert.True(success);

            Assert.NotNull(result.A);
            Assert.NotNull(result.B);
            Assert.NotNull(result.C);
            Assert.NotNull(result.D);

            Assert.Equal(4, result.A.Length);
            Assert.Equal([1, 2, 3, 4], result.A);

            Assert.Equal(3, result.BLength);
            Assert.Equal(result.BLength, result.B.Length);
            Assert.Equal([12, 13, 14], result.B);

            Assert.Equal(4, result.C.Length);
            Assert.Equal([34, 33, 32, 31], result.C);

            Assert.Equal(4, result.D.Length);
            Assert.Equal([41, 42, 43, 44], result.D);
        }
    }
}
