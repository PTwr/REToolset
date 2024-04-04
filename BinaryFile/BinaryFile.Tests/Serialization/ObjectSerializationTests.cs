using BinaryFile.Unpacker.Marshalers;
using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using BinaryDataHelper;

namespace BinaryFile.Tests.Serialization
{
    public class ObjectSerializationTests
    {
        private class POCORoot
        {
            public byte A { get; set; } = 1;
            public byte B { get; set; } = 2;

            public POCOChild Child { get; set; } = new POCOChild();

            public class POCOChild
            {
                public byte A { get; set; } = 3;
                public byte B { get; set; } = 4;

                public POCOGrandChild GrandChild { get; set; } = new POCOGrandChild();

                public class POCOGrandChild
                {
                    public byte A { get; set; } = 5;
                    public byte B { get; set; } = 6;
                }
            }
        }

        private static void PrepBasicTypemap(out RootMarshalingContext ctx, out FluentMarshaler<POCORoot> root, out FluentMarshaler<POCORoot.POCOChild> child, out FluentMarshaler<POCORoot.POCOChild.POCOGrandChild> grandchild)
        {
            var mgr = new MarshalerManager();
            ctx = new RootMarshalingContext(mgr, mgr);

            root = new FluentMarshaler<POCORoot>();
            root.WithField<byte>("A").AtOffset(0).From((i) => i.A);
            root.WithField<byte>("B").AtOffset(1).From((i) => i.B);

            child = new FluentMarshaler<POCORoot.POCOChild>();
            child.WithField<byte>("A").AtOffset(0).From((i) => i.A);
            child.WithField<byte>("B").AtOffset(1).From((i) => i.B);

            grandchild = new FluentMarshaler<POCORoot.POCOChild.POCOGrandChild>();
            grandchild.WithField<byte>("A").AtOffset(0).From((i) => i.A);
            grandchild.WithField<byte>("B").AtOffset(1).From((i) => i.B);

            mgr.Register(root);
            mgr.Register(child);
            mgr.Register(grandchild);
            mgr.Register(new IntegerMarshaler());
        }

        [Fact]
        public void SimplePOCOTest()
        {
            PrepBasicTypemap(out var ctx, out var root, out var child, out var grandchild);

            var expected = new byte[]
            {
                1,2,
            };
            var obj = new POCORoot();

            var buffer = new ByteBuffer();
            root.Serialize(obj, buffer, ctx, out var l);

            var actual = buffer.GetData();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SimplePOCOWithChildTest()
        {
            PrepBasicTypemap(out var ctx, out var root, out var child, out var grandchild);
            root.WithField<POCORoot.POCOChild>("Child").AtOffset(2).From(i => i.Child);

            var expected = new byte[]
            {
                1,2,
                3,4,
            };
            var obj = new POCORoot();

            var buffer = new ByteBuffer();
            root.Serialize(obj, buffer, ctx, out var l);

            var actual = buffer.GetData();

            Assert.Equal(expected, actual);
        }
        [Fact]
        public void SimplePOCOWithGrandChildTest()
        {
            PrepBasicTypemap(out var ctx, out var root, out var child, out var grandchild);
            root.WithField<POCORoot.POCOChild>("Child").AtOffset(2).From(i => i.Child);
            child.WithField<POCORoot.POCOChild.POCOGrandChild>("GrandChild").AtOffset(2).From(i => i.GrandChild);

            var expected = new byte[]
            {
                1,2,
                3,4,
                5,6,
            };
            var obj = new POCORoot();

            var buffer = new ByteBuffer();
            root.Serialize(obj, buffer, ctx, out var l);

            var actual = buffer.GetData();

            Assert.Equal(expected, actual);
        }
        [Fact]
        public void SimplePOCOWithChildCollectionTest()
        {

        }
    }
}
