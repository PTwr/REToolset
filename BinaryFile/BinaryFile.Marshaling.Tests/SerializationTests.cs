using BinaryDataHelper;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.PrimitiveMarshaling;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.Tests
{
    public class SerializationTests
    {
        class Unary
        {
            public byte A { get; set; }
            public byte B { get; set; }
            public ItemBase ItemA { get; set; }
            public ItemBase ItemB { get; set; }
        }
        class ItemBase
        {
            public byte X { get; set; }
            public byte Y { get; set; }
        }
        class ItemA : ItemBase
        {
            public ushort WORDA { get; set; }
            public ushort WORDB { get; set; }
        }
        class ItemB : ItemBase
        {
            public uint DWORD { get; set; }
        }

        [Fact]
        public void UnarySerialization()
        {
            var a = new Unary()
            {
                A = 9,
                B = 8,
                ItemA = new ItemA()
                {
                    X = 7,
                    Y = 6,
                    WORDA = 0x0102,
                    WORDB = 0x0304,
                },
                ItemB = new ItemB()
                {
                    X = 5,
                    Y = 4,
                    DWORD = 0x05060708,
                },
            };

            byte[] expected = [
                9, 8,
                7, 6, 1, 2, 3, 4,
                5, 4, 5, 6, 7, 8,
                ];

            IMarshalerStore store = new MarshalerStore();
            store.Register(new IntegerMarshaler());

            var mapUnary = new RootTypeMarshaler<Unary>();

            mapUnary.WithField(i => i.A).AtOffset(0);
            mapUnary.WithField(i => i.B).AtOffset(1);
            mapUnary.WithField(i => i.ItemA).AtOffset(2);
            mapUnary.WithField(i => i.ItemB).AtOffset(8);

            var mapItem = new RootTypeMarshaler<ItemBase>();
            mapItem.WithField(i => i.X).AtOffset(0);
            mapItem.WithField(i => i.Y).AtOffset(1);

            var mapItemA = mapItem.Derive<ItemA>();
            mapItemA.WithField(i => i.WORDA).AtOffset(2);
            mapItemA.WithField(i => i.WORDB).AtOffset(4);

            var mapItemB = mapItem.Derive<ItemB>();
            mapItemB.WithField(i => i.DWORD).AtOffset(2);

            store.Register(mapUnary);
            store.Register(mapItem);

            var buffer = new ByteBuffer();

            var rootCtx = new RootMarshalingContext(store);
            var d = store.FindMarshaler(a);

            d.Serialize(a, buffer, rootCtx, out _);

            Assert.Equal(expected, buffer.GetData());
        }
    }
}
