using BinaryFile.Unpacker.Marshalers;
using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Tests.Deserialization
{
    public class FluentRecursiveDeserializationTests
    {
        interface IPOCOWithItems
        {
            List<POCO.Item> Items { get; }
        }
        class POCO : IPOCOWithItems
        {
            public byte ItemCount { get; set; }

            public List<Item> Items { get; set; } = new List<Item>();

            public class Item : IPOCOWithItems, IBinarySegment<IPOCOWithItems>
            {
                public override string ToString()
                {
                    return $"{ExpectedId} {LastNodeId}";
                }

                public byte CalculatedId { get; set; }
                public byte ExpectedId { get; set; }
                public byte LastNodeId { get; set; }

                public List<Item> Items { get; set; } = new List<Item>();

                public IPOCOWithItems? Parent => throw new NotImplementedException();
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
                   4, 9,
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
            var itemD = new FluentMarshaler<POCO.Item>();

            mgr.Register(new IntegerMarshaler());
            mgr.Register(pocoD);
            mgr.Register(itemD);

            itemD.WithField<byte>("ExpectedId")
                .AtOffset(0)
                .Into((poco, x) => poco.ExpectedId = x);
            itemD.WithField<byte>("LastNodeId")
                .AtOffset(1)
                .Into((poco, x) => poco.LastNodeId = x);

            pocoD.WithField<byte>("ItemCount")
                .AtOffset(0)
                //its good to ensure such fields are deserialized before they are needed
                .InDeserializationOrder(-1)
                .Into((poco, x) => poco.ItemCount = x);
            pocoD.WithCollectionOf<POCO.Item>("Items")
                .AtOffset(1)
                .WithCountOf((poco) => poco.ItemCount)
                .WithItemLengthOf(2)
                .Into((poco, item, index, offset) =>
                {
                    Assert.Equal(item.ExpectedId, index);
                    poco.Items.Add(item);
                })
                .Into((poco, x) => { });

            var result = pocoD.Deserialize(bytes.AsSpan(), ctx, out var l);
        }

        private static POCO PrepPOCO()
        {
            var obj = new POCO();
            obj.ItemCount = 10;
            obj.Items = [
                new POCO.Item() {
                    ExpectedId = 0,
                    LastNodeId = 8,
                    Items = [
                        new POCO.Item(){
                            ExpectedId = 1,
                            LastNodeId = 1,
                        },
                        new POCO.Item(){
                            ExpectedId = 2,
                            LastNodeId = 3,
                            Items = [
                                new POCO.Item() {
                                    ExpectedId = 3,
                                    LastNodeId = 3,
                                },
                                ],
                        },
                        new POCO.Item(){
                            ExpectedId = 4,
                            LastNodeId = 8,
                            Items = [
                                new POCO.Item() {
                                    ExpectedId = 5,
                                    LastNodeId = 5,
                                },
                                new POCO.Item() {
                                    ExpectedId = 6,
                                    LastNodeId = 8,
                                    Items = [
                                        new POCO.Item() {
                                            ExpectedId = 7,
                                            LastNodeId = 7,
                                        },
                                        new POCO.Item() {
                                            ExpectedId = 8,
                                            LastNodeId = 8,
                                        },
                                    ],
                                },
                            ],
                        },
                    ],
                },
                new POCO.Item() {
                    ExpectedId = 9,
                    LastNodeId = 9,
                },
            ];

            return obj;
        }
    }
}
