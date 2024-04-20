using BinaryFile.Marshaling.Activation;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.PrimitiveMarshaling;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.Tests
{
    public class DeserializationTests
    {
        class SingleLevel
        {
            public byte A { get; set; }
            public byte B { get; set; }
            public byte C { get; set; }
            public byte D { get; set; }
        }

        [Fact]
        public void SingleLevelDeserialization()
        {
            IMarshalerStore store = new MarshalerStore();
            var map = new TypeMarshaler<SingleLevel, SingleLevel, SingleLevel>();
            store.Register(map);
            var intMap = new IntegerMarshaler();
            store.Register(intMap);

            map.WithField(i => i.A).AtOffset(0);
            map.WithField(i => i.B).AtOffset(1);
            map.WithField(i => i.C).AtOffset(2);
            map.WithField(i => i.D).AtOffset(3);

            byte[] bytes = [1, 2, 3, 4];

            var d = store.FindMarshaler<SingleLevel>();

            var rootCtx = new MarshalingContext("root", store, null, 0, Common.OffsetRelation.Absolute, null);
            var x = d.Deserialize(null, null, bytes.AsMemory(), rootCtx, out _);

            Assert.IsType<SingleLevel>(x);

            Assert.Equal(1, x.A);
            Assert.Equal(2, x.B);
            Assert.Equal(3, x.C);
            Assert.Equal(4, x.D);
        }

        class FirstNesting
        {
            public byte X { get; set; }
            public byte Y { get; set; }
            public byte Z { get; set; }

            public SingleLevel SingleLevel { get; set; }
        }

        [Fact]
        public void OneNestingDeserialization()
        {
            IMarshalerStore store = new MarshalerStore();
            var mapSingleLevel = new TypeMarshaler<SingleLevel, SingleLevel, SingleLevel>();
            var firstNestingMap = new TypeMarshaler<FirstNesting, FirstNesting, FirstNesting>();
            store.Register(mapSingleLevel);
            store.Register(firstNestingMap);
            var intMap = new IntegerMarshaler();
            store.Register(intMap);

            mapSingleLevel.WithField(i => i.A).AtOffset(0);
            mapSingleLevel.WithField(i => i.B).AtOffset(1);
            mapSingleLevel.WithField(i => i.C).AtOffset(2);
            mapSingleLevel.WithField(i => i.D).AtOffset(3);

            firstNestingMap.WithField(i => i.X).AtOffset(0);
            firstNestingMap.WithField(i => i.Y).AtOffset(1);
            firstNestingMap.WithField(i => i.Z).AtOffset(2);
            firstNestingMap.WithField(i => i.SingleLevel).AtOffset(3);

            byte[] bytes = [
                9, 8, 7,
                1, 2, 3, 4];

            var d = store.FindMarshaler<FirstNesting>();

            var rootCtx = new MarshalingContext("root", store, null, 0, Common.OffsetRelation.Absolute, null);
            var y = d.Deserialize(null, null, bytes.AsMemory(), rootCtx, out _);

            Assert.IsType<FirstNesting>(y);
            Assert.NotNull(y.SingleLevel);
            var x = y.SingleLevel;

            Assert.Equal(9, y.X);
            Assert.Equal(8, y.Y);
            Assert.Equal(7, y.Z);

            Assert.IsType<SingleLevel>(x);

            Assert.Equal(1, x.A);
            Assert.Equal(2, x.B);
            Assert.Equal(3, x.C);
            Assert.Equal(4, x.D);
        }

        class SingleLevelB : SingleLevel
        {
            public int DWORD { get; set; }
        }
        [Fact]
        public void OneNestingDeserializationWithCondition()
        {
            IMarshalerStore store = new MarshalerStore();
            var mapSingleLevel = new TypeMarshaler<SingleLevel, SingleLevel, SingleLevel>();
            var firstNestingMap = new TypeMarshaler<FirstNesting, FirstNesting, FirstNesting>();
            store.Register(mapSingleLevel);
            store.Register(firstNestingMap);
            var intMap = new IntegerMarshaler();
            store.Register(intMap);

            var derrivedSingleLevelMap = mapSingleLevel.Derive<SingleLevelB>();

            derrivedSingleLevelMap.WithField(i => i.DWORD).AtOffset(0);

            mapSingleLevel.WithField(i => i.A).AtOffset(0);
            mapSingleLevel.WithField(i => i.B).AtOffset(1);
            mapSingleLevel.WithField(i => i.C).AtOffset(2);
            mapSingleLevel.WithField(i => i.D).AtOffset(3);

            firstNestingMap.WithField(i => i.X).AtOffset(0);
            firstNestingMap.WithField(i => i.Y).AtOffset(1);
            firstNestingMap.WithField(i => i.Z).AtOffset(2);
            firstNestingMap.WithField(i => i.SingleLevel).AtOffset(3);

            var ca = new CustomActivator<SingleLevelB>((data, ctx) =>
            {
                if (ctx.ItemSlice(data).Span[0] == 1)
                    return new SingleLevelB();
                return null;
            });

            //TODO helper method to do it fluently
            mapSingleLevel.WithCustomActivator(ca);

            byte[] bytes = [
                9, 8, 7,
                1, 2, 3, 4];

            var d = store.FindMarshaler<FirstNesting>();

            var rootCtx = new MarshalingContext("root", store, null, 0, Common.OffsetRelation.Absolute, null);
            var y = d.Deserialize(null, null, bytes.AsMemory(), rootCtx, out _);

            Assert.IsType<FirstNesting>(y);
            Assert.NotNull(y.SingleLevel);
            var x = y.SingleLevel;

            Assert.Equal(9, y.X);
            Assert.Equal(8, y.Y);
            Assert.Equal(7, y.Z);

            Assert.IsType<SingleLevelB>(x);

            Assert.Equal(1, x.A);
            Assert.Equal(2, x.B);
            Assert.Equal(3, x.C);
            Assert.Equal(4, x.D);

            var z = x as SingleLevelB;

            Assert.Equal(0x01020304, z.DWORD);
        }

        class CollectionItem
        {
            public byte A { get; set; }
            public byte B { get; set; }
        }
        class CollectionContainer
        {
            public byte Count { get; set; }
            public List<CollectionItem> Items { get; set; }
        }
        [Fact]
        public void CollectionDeserialization()
        {
            IMarshalerStore store = new MarshalerStore();
            var ctx = new RootMarshalingContext(store);

            var containerMap = new RootTypeMarshaler<CollectionContainer>();
            var itemMap = new RootTypeMarshaler<CollectionItem>();

            store.Register(containerMap); store.Register(itemMap); store.Register(new IntegerMarshaler());

            containerMap.WithField(i => i.Count).AtOffset(0);
            containerMap.WithCollectionOf(i => i.Items).AtOffset(1)
                .WithItemByteLengthOf(2)
                .WithCountOf(container => container.Count);

            itemMap.WithField(i => i.A).AtOffset(0);
            itemMap.WithField(i => i.B).AtOffset(1);

            byte[] bytes = [
                3,
                9, 8,
                7, 6,
                5, 4,
                ];

            var r = store.FindMarshaler<CollectionContainer>().Deserialize(null, null, bytes.AsMemory(), ctx, out _);

            Assert.Equal(3, r.Count);
            Assert.Equal(3, r.Items.Count);

            Assert.Equal(9, r.Items[0].A);
            Assert.Equal(8, r.Items[0].B);

            Assert.Equal(7, r.Items[1].A);
            Assert.Equal(6, r.Items[1].B);

            Assert.Equal(5, r.Items[2].A);
            Assert.Equal(4, r.Items[2].B);
        }
    }
}
