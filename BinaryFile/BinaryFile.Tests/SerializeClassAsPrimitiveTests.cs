using BinaryFile.Unpacker.Marshalers;
using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryDataHelper;

namespace BinaryFile.Tests
{
    public class SerializeClassAsPrimitiveTests
    {
        //structure similar to U8 format
        class Container
        {
            public byte A { get; set; }
            public byte B { get; set; }

            //recalculated from data during serialization!
            public byte ItemCount { get; set; }
            //recalculated from data during serialization!
            public byte StringStorageOffset { get; set; }
            //recalculated from data during serialization!
            public byte ItemsAndTheirStorageByteLength { get; set; }

            public List<Item> Items { get; set; }

            //to be placed after Items and their out-of-segment storage
            //offset = itemsOffset + ItemsAndTheirStorageByteLength
            public byte Footer { get; set; }

        }
        class Item
        {
            public Item(Container container)
            {
                Container = container;
            }

            public Container Container { get; set; }

            public byte Id { get; set; }
            //recalculated from data during serialization!
            public byte StringRelativeOffset { get; set; }
            //recalculated from data during serialization!
            public byte StringLength { get; set; }
            public string S { get; set; }
        }

        [Fact]
        public void CollectionWithOutOfSegmentStorageTest()
        {
            var marshalerContainer = new FluentMarshaler<Container>();
            var marshalerItem = new FluentMarshaler<Item>();

            marshalerItem
                .WithField<byte>("Id")
                .AtOffset(0)
                .From(item => item.Id);
            marshalerItem
                .WithField<byte>("StringRelativeOffset")
                .AtOffset(1)
                .From(item => item.StringRelativeOffset);
            marshalerItem
                .WithField<byte>("StringLength")
                .AtOffset(2)
                .From(item => item.StringLength);

            marshalerContainer
                .WithField<byte>("A")
                .AtOffset(0)
                .From((container) => container.A);
            marshalerContainer
                .WithField<byte>("B")
                .AtOffset(1)
                .From((container) => container.B);
            //TODO would it be codesmell for recalculated fields to be updated during their serialization?
            //TODO with enough helpers it could be
            //TODO marshaler.WithField(x=>x.Field).AtOffset(123).DynamicallyRecalculate((x,f)=>x.f = ...)
            //TODO unlike calculating in getters or manualy invoking .Update() such recalculation would be hard-linked to serialization order
            marshalerContainer
                .WithField<byte>("ItemCount")
                .AtOffset(2)
                .From((container) =>
                {
                    container.ItemCount = (byte)container.Items.Count;
                    return container.ItemCount;
                });
            marshalerContainer
                .WithField<byte>("String Storage Offset")
                .AtOffset(3)
                .From((container) =>
                {
                    //offset 4 is nodes+storage length, which needs storage serialization to calculate data byte length
                    container.StringStorageOffset = (byte)(5 + container.Items.Count * 3);
                    return container.StringStorageOffset;
                });

            marshalerContainer
                //marshal only string value from each item
                .WithCollectionOf<Item, string>("Items storage")
                //in order to trust serialization-time calculated fields manual ordering is advised
                //framework is preserving fieldDescriptor order, but without manual ordering shit will fall apart after refactor of config :)
                .InSerializationOrder(10)
                .AtOffset(container => container.StringStorageOffset)
                .WithNullTerminator(true)
                //while for AsCII byte-length could be calculated from string-length, for variable-width encodings like Shift-JIS its not so easy
                .WithEncoding(Encoding.ASCII)
                .From(container => container.Items)
                //marshall just single field in this enumeration, not whole object
                .WithMarshalingValueGetter((container, item) => item.S)
                .AfterSerializing((container, item, byteLength, relativeOffset) =>
                {
                    //segment relative offset, which in this case is same as absolute as strings are stored through root object context
                    //making it relative to item space or string space start is simple math
                    item.StringRelativeOffset = (byte)relativeOffset;
                    item.StringLength = (byte)byteLength;
                })
                .AfterSerializing((container, byteLength) => 
                    container.ItemsAndTheirStorageByteLength = (byte)(container.ItemCount * 3 + byteLength)
                );

            marshalerContainer
                .WithField<byte>("ItemsAndTheirStorageByteLength")
                .AtOffset(4)
                .InSerializationOrder(15) //after strings got serialized and their bytes counted
                .From(container => container.ItemsAndTheirStorageByteLength);

            marshalerContainer
                .WithCollectionOf<Item>("Items")
                .InSerializationOrder(20) //after string storage recalculates string offsets
                .AtOffset(5)
                //.WithCountOf(container => container.ItemCount) //unnecessary as long as all items are serialized
                //TODO allow setting Type Length on FluentMarshaler level
                .WithItemLengthOf(3)
                .From(container => container.Items)
                .AfterSerializing((container, length) =>
                {
                    //if item length would not be constant, length could be used to calculate offsets of following sections
                });

            marshalerContainer
                .WithField<byte>("Footer")
                .InSerializationOrder(30) //all dynamic lengths must be calculated before footer offset can be calculated
                .AtOffset(container => container.ItemsAndTheirStorageByteLength + 5) //"header" is 5 bytes long
                .From(container => container.Footer);


            var mgr = new MarshalerManager();
            var ctx = new RootMarshalingContext(mgr, mgr);
            mgr.Register(marshalerContainer);
            mgr.Register(marshalerItem);
            mgr.Register(new IntegerMarshaler());
            mgr.Register(new StringMarshaler());
            mgr.Register(new BinaryArrayMarshaler());

            var container = new Container()
            {
                A = 1,
                B = 2,
                Footer = 255,
            };
            container.Items = new List<Item>()
            {
                new Item(container)
                {
                    Id = 1,
                    S = "abcd",
                },
                new Item(container)
                {
                    Id = 2,
                    S = "1234567890",
                },
                new Item(container)
                {
                    Id = 3,
                    S = "lorem ipsum",
                },
                new Item(container)
                {
                    Id = 4,
                    S = "foo bar",
                },
            };

            var buffer = new ByteBuffer();
            marshalerContainer.Serialize(container, buffer, ctx, out var l);

            var actual = buffer.GetData();

            byte[] expected = [
	            0x01, 0x02, //A B
                0x04, //Item Count
                0x11, //string storage offset
                0x30, //items+string byte length
                0x01, 0x11, 0x05, //ID, string offset (absolute), string length
                0x02, 0x16, 0x0B, //---
                0x03, 0x21, 0x0C, //---
                0x04, 0x2D, 0x08, //---
                0x61, 0x62, 0x63, 0x64, 0x00, //abcd
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30, 0x00, //1234567890
                0x6C, 0x6F, 0x72, 0x65, 0x6D, 0x20, 0x69, 0x70, 0x73, 0x75, 0x6D, 0x00, //lorem ipsum
                0x66, 0x6F, 0x6F, 0x20, 0x62, 0x61, 0x72, 0x00, //foo bar
                0xFF, //footer
            ];

            Assert.Equal(expected, actual);
        }
    }
}
