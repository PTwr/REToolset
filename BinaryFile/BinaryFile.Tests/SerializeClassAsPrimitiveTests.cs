using BinaryFile.Unpacker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryDataHelper;
using BinaryFile.Unpacker.New.Implementation;
using BinaryFile.Unpacker.New.Implementation.PrimitiveMarshalers;
using BinaryFile.Unpacker.New.Implementation.ObjectMarshalers;
using BinaryFile.Unpacker.New.Interfaces;
using System.Runtime.InteropServices;
using BinaryFile.Unpacker.New.Implementation.ObjectMarshalers.FieldMarshalers;

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
            public byte StringAbsoluteOffset { get; set; }
            //recalculated from data during serialization!
            public byte StringLength { get; set; }
            public string S { get; set; }
        }

        [Fact]
        public void CollectionWithOutOfSegmentStorageTest()
        {
            var store = new MarshalerStore();

            store.RegisterPrimitiveMarshaler(new IntegerMarshaler());
            store.RegisterPrimitiveMarshaler(new StringMarshaler());
            store.RegisterPrimitiveMarshaler(new IntegerArrayMarshaler());

            var mapContainer = new TypeMarshaler<Container>();
            store.RegisterRootMap(mapContainer);

            mapContainer.WithField("A", x => x.A).AtOffset(0);
            mapContainer.WithField("B", x => x.B).AtOffset(1);

            mapContainer.WithField("ItemCount", x => x.ItemCount).AtOffset(2)
                .From(container =>
                {
                    container.ItemCount = (byte)(container.Items?.Count ?? 0);
                    return container.ItemCount;
                });
            mapContainer.WithField("String Storage Offset", x => x.StringStorageOffset).AtOffset(3)
                .From(container =>
                {
                    container.StringStorageOffset = (byte)(5 + container.Items.Count * 3);
                    return container.StringStorageOffset;
                });
            mapContainer.WithField("ItemsAndTheirStorageByteLength", x => x.ItemsAndTheirStorageByteLength)
                .WithDeserializationOrderOf(0)
                //has to be recalculated from string lengths, after Items got serialized
                .WithSerializationOrderOf(15)
                .AtOffset(4)
                .From(x =>
                {
                    //recalculate and store
                    x.ItemsAndTheirStorageByteLength = (byte)(x.Items?.Sum(i => 3 + i.StringLength) ?? 0);
                    return x.ItemsAndTheirStorageByteLength;
                });

            var itemStringsDescriptor = new OrderedCollectionFieldMarshaler<Container, Item, string>("Item Strings");
            mapContainer.WithMarshalingAction(itemStringsDescriptor);

            itemStringsDescriptor
                .AtOffset(x => x.StringStorageOffset)
                .WithSerializationOrderOf(10)
                .WithEncoding(Encoding.ASCII)
                .WithNullTerminator()
                .From(c => c.Items)
                .MarshalFrom((c, i) => i.S)
                //can be done for field/collection as a whole
                //.AfterSerializing((c, l) => c.ItemsAndTheirStorageByteLength = (byte)(c.ItemCount * 3 + l))
                //or on per-item basis
                .AfterSerializingItem((c, i, l, n, o) =>
                {
                    i.StringAbsoluteOffset = (byte)(o + c.StringStorageOffset);
                    i.StringLength = (byte)l;
                });

            //TODO helper method
            var itemCollectionDescriptor = new OrderedCollectionFieldMarshaler<Container, Item, Item>("Items");
            mapContainer.WithMarshalingAction(itemCollectionDescriptor);

            itemCollectionDescriptor
                .AtOffset(5) //after header
                .WithSerializationOrderOf(20) //after string length/offset gets recalculated
                .WithCountOf(c => c.ItemCount)
                .WithItemByteLengthOf(3)
                //TODO helper to autofil for TFieldType = TMarshalingType
                .MarshalFrom((a, b) => b)
                .MarshalInto((a, b, c) => c)
                .From(c => c.Items)
                .Into((c, e) => c.Items = e.ToList());

            mapContainer.WithField("Footer", x => x.Footer)
                .AtOffset(c => c.ItemsAndTheirStorageByteLength + 5) //5 byte header
                .RelativeTo(OffsetRelation.Absolute)
                .WithSerializationOrderOf(30); //allow Item serialization to recalculate total length

            var mapItem = new TypeMarshaler<Item>();
            store.RegisterRootMap(mapItem);

            mapItem.WithField("Id", x => x.Id).AtOffset(0);
            mapItem.WithField("Str Offset", x => x.StringAbsoluteOffset).AtOffset(1);
            mapItem.WithField("Str Length", x => x.StringLength).AtOffset(2);
            //string offset depends on previous item, so its easiest to serialize them as their own list
            mapItem.WithField("Str", x => x.S, serialize: false)
                .AtOffset(x => x.StringAbsoluteOffset)
                .RelativeTo(OffsetRelation.Absolute)
                .WithDeserializationOrderOf(10) //after offset/length is read
                .WithByteLengthOf(x => x.StringLength)
                .WithEncoding(Encoding.ASCII)
                .WithNullTerminator();

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

            var rootCtx = new MarshalingContext("root", store, null, 0, OffsetRelation.Absolute, null);
            ByteBuffer buffer = new ByteBuffer();

            var des = store.GetDeserializatorFor<Container>();
            var obj = des.DeserializeInto(new Container(), expected.AsSpan(), rootCtx, out _);

            // deserialize-serialize loop
            var ser = store.GetSerializatorFor<Container>();
            ser.SerializeFrom(obj, buffer, rootCtx, out _);

            var actual = buffer.GetData();

            Assert.Equal(expected, actual);

            //TODO fast zerofill on existing buffer?
            buffer = new ByteBuffer();

            //Serialize from clean object having no offset/length
            ser.SerializeFrom(container, buffer, rootCtx, out _);

            actual = buffer.GetData();
            Assert.Equal(expected, actual);
        }
    }
}
