using BinaryFile.Unpacker.Marshalers;
using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace BinaryFile.Tests.Deserialization
{
    public class FluentRecursiveDeserializationTests
    {
        interface IPOCOWithItemsA
        {
            List<POCO.ItemA> ItemsA { get; }
        }
        interface IPOCOWithItemsB
        {
            List<POCO.ItemB> ItemsB { get; }
            IEnumerable<POCO.ItemB> FlattenedItemsB { get; }
        }
        class POCO : IPOCOWithItemsA, IPOCOWithItemsB
        {
            public byte ItemCount { get; set; }

            public List<ItemA> ItemsA { get; set; } = new List<ItemA>();
            public List<ItemB> ItemsB { get; set; } = new List<ItemB>();

            public IEnumerable<ItemA> FlattenedItems =>
                ItemsA.SelectMany(i => i.Descendants);

            public IEnumerable<ItemB> FlattenedItemsB => ItemsB.SelectMany(i => i.FlattenedItemsB);

            public class ItemB : IPOCOWithItemsB, IBinarySegment<IPOCOWithItemsB>
            {
                public ItemB(IPOCOWithItemsB parent)
                {
                    Parent = parent;
                }
                public IEnumerable<ItemB> FlattenedItemsB => [this, .. ItemsB.SelectMany(x => x.FlattenedItemsB)];

                public override string ToString()
                {
                    return $"{ExpectedId} {LastNodeId}";
                }

                public void SetParent(IPOCOWithItemsB parent) => Parent = parent;
                public IPOCOWithItemsB Parent { get; protected set; }

                public byte CalculatedId { get; set; }
                public byte ExpectedId { get; set; }
                public byte LastNodeId { get; set; }

                public List<ItemB> ItemsB { get; set; } = new List<ItemB>();

                //item + children length
                public int SliceLength => (LastNodeId - ExpectedId + 1) * 2;
            }
            public class ItemA : IPOCOWithItemsA, IBinarySegment<IPOCOWithItemsA>
            {
                public IEnumerable<ItemA> Descendants => [this, .. ItemsA.SelectMany(x => x.Descendants)];

                public override string ToString()
                {
                    return $"{ExpectedId} {LastNodeId}";
                }

                public byte CalculatedId { get; set; }
                public byte ExpectedId { get; set; }
                public byte LastNodeId { get; set; }

                public List<ItemA> ItemsA { get; set; } = new List<ItemA>();

                public void SetParent(IPOCOWithItemsA parent) => Parent = parent;
                public IPOCOWithItemsA? Parent { get; protected set; }
            }
        }

        [Fact]
        public void DeserializeRecursiveLitTest()
        {
            var bytes = new byte[]
            {
                10, //item count
                0, 8,
                   1, 1,
                   2, 3,
                      3, 3,
                   4, 8,
                      5, 5,
                      6, 8,
                         7, 7,
                         8, 8,
                9, 9,
            };

            var expected = PrepPOCO();

            var mgr = new MarshalerManager();
            var ctx = new RootMarshalingContext(mgr, mgr);
            var pocoD = new FluentMarshaler<POCO>();
            var itemDA = new FluentMarshaler<POCO.ItemA>();
            var itemDB = new FluentMarshaler<POCO.ItemB>();

            mgr.Register(new IntegerMarshaler());
            mgr.Register(pocoD);
            mgr.Register(itemDA);
            mgr.Register(itemDB);

            itemDA.WithField<byte>("ExpectedId")
                .AtOffset(0)
                .Into((poco, x) => poco.ExpectedId = x);
            itemDA.WithField<byte>("LastNodeId")
                .AtOffset(1)
                .Into((poco, x) => poco.LastNodeId = x);

            pocoD.WithField<byte>("ItemCount")
                .AtOffset(0)
                //its good to ensure such fields are deserialized before they are needed
                .InDeserializationOrder(-1)
                .Into((poco, x) => poco.ItemCount = x);
            pocoD.WithCollectionOf<POCO.ItemA>("ItemsA")
                .AtOffset(1)
                .WithCountOf((poco) => poco.ItemCount)
                .WithItemLengthOf(2)
                .Into((poco, item, marshaled, index, offset) =>
                {
                    IPOCOWithItemsA parent =
                        poco.FlattenedItems
                            .Where(i => i.LastNodeId >= index)
                            .Cast<IPOCOWithItemsA>()
                            .LastOrDefault()
                        ?? poco;

                    Assert.Equal(item.ExpectedId, index);
                    item.SetParent(parent);
                    parent.ItemsA.Add(item);
                })
                .Into((poco, x) => { });
            pocoD.WithCollectionOf<POCO.ItemB>("RootItemsB")
                .AtOffset(1)
                //limit field byte length instead of item count
                .WithLengthOf((poco) => poco.ItemCount * 2)
                //length of top level children, to skip over deeper descendants
                .WithItemLengthOf((poco, item) => item.SliceLength)
                .Into((poco, item, marshaled, index, offset) => poco.ItemsB.Add(item));

            itemDB.WithField<byte>("ExpectedId")
                .AtOffset(0)
                .Into((itemB, x) => itemB.ExpectedId = x);
            itemDB.WithField<byte>("LastNodeId")
                .AtOffset(1)
                .Into((itemB, x) =>
                {
                    itemB.CalculatedId = (byte)((itemB.Parent.FlattenedItemsB.LastOrDefault()?.CalculatedId ?? -1) + 1);
                    itemB.LastNodeId = x;
                });
            itemDB.WithCollectionOf<POCO.ItemB>("ItemsB")
                .AtOffset(2)
                //length of all descendants combined
                .WithLengthOf((poco) => poco.SliceLength - 2)
                //length of top level children, to skip over deeper descendants
                .WithItemLengthOf((poco, child) => child.SliceLength)
                .Into((poco, item, marshaled, index, offset) => poco.ItemsB.Add(item));

            var result = pocoD.Deserialize(bytes.AsSpan(), ctx, out var l);

            //TODO some assert :D
        }

        private static POCO PrepPOCO()
        {
            var obj = new POCO();
            obj.ItemCount = 10;
            obj.ItemsA = [
                new POCO.ItemA() {
                    ExpectedId = 0,
                    LastNodeId = 8,
                    ItemsA = [
                        new POCO.ItemA(){
                            ExpectedId = 1,
                            LastNodeId = 1,
                        },
                        new POCO.ItemA(){
                            ExpectedId = 2,
                            LastNodeId = 3,
                            ItemsA = [
                                new POCO.ItemA() {
                                    ExpectedId = 3,
                                    LastNodeId = 3,
                                },
                                ],
                        },
                        new POCO.ItemA(){
                            ExpectedId = 4,
                            LastNodeId = 8,
                            ItemsA = [
                                new POCO.ItemA() {
                                    ExpectedId = 5,
                                    LastNodeId = 5,
                                },
                                new POCO.ItemA() {
                                    ExpectedId = 6,
                                    LastNodeId = 8,
                                    ItemsA = [
                                        new POCO.ItemA() {
                                            ExpectedId = 7,
                                            LastNodeId = 7,
                                        },
                                        new POCO.ItemA() {
                                            ExpectedId = 8,
                                            LastNodeId = 8,
                                        },
                                    ],
                                },
                            ],
                        },
                    ],
                },
                new POCO.ItemA() {
                    ExpectedId = 9,
                    LastNodeId = 9,
                },
            ];

            return obj;
        }
    }
}
