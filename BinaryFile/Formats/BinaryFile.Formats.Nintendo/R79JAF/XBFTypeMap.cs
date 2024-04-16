using BinaryFile.Unpacker.New.Implementation;
using BinaryFile.Unpacker.New.Implementation.ObjectMarshalers;

namespace BinaryFile.Formats.Nintendo.R79JAF
{
    public static class XBFTypeMap
    {
        public static void Register(MarshalerStore store)
        {
            var XBFmap = new TypeMarshaler<XBF>();
            var XBFNodeMap = new TypeMarshaler<XBF.XBFTreeNode>();

            store.RegisterRootMap(XBFmap);
            store.RegisterRootMap(XBFNodeMap);

            XBFNodeMap.WithField(x => x.NameOrAttributeId).AtOffset(0);
            XBFNodeMap.WithField(x => x.ValueId).AtOffset(2);

            XBFmap.WithField(x => x.Magic1).AtOffset(0)
                .WithExpectedValueOf(XBF.MagicNumber1);
            XBFmap.WithField(x => x.Magic2).AtOffset(4)
                .WithExpectedValueOf(XBF.MagicNumber2);
            XBFmap.WithField(x => x.TreeStructureOffset).AtOffset(8)
                .WithExpectedValueOf(XBF.ExpectedTreeStructureOffset);

            XBFmap.WithField(x => x.TreeStructureCount).AtOffset(12)
                .From(i => i.TreeStructure.Count);

            XBFmap.WithField(x => x.TagListOffset).AtOffset(16)
                .WithSerializationOrderOf(10) //after Tree Structure
                .From((i) => XBF.ExpectedTreeStructureOffset + i.TreeStructure.Count * 4);
            XBFmap.WithField(x => x.TagListCount).AtOffset(20)
                .From((i) => i.TagList.Count);

            XBFmap.WithField(x => x.AttributeListOffset).AtOffset(24)
                .WithSerializationOrderOf(20); //after tag list
            XBFmap.WithField(x => x.AttributeListCount).AtOffset(28)
                .From((i) => i.AttributeList.Count);

            XBFmap.WithField(x => x.ValueListOffset).AtOffset(32)
                .WithSerializationOrderOf(30); //after attribute list
            XBFmap.WithField(x => x.ValueListCount).AtOffset(36)
                .From((i) => i.ValueList.Count);

            XBFmap
                .WithCollectionOf(x => x.TreeStructure)
                .WithSerializationOrderOf(1) //before list offsets
                .AtOffset(i => i.TreeStructureOffset)
                .WithCountOf(i => i.TreeStructureCount)
                .WithByteLengthOf(i => i.TreeStructureLength)
                .WithItemByteLengthOf(4)
                .From(i => i.TreeStructure)
                .AfterSerializing((xbf, l) => xbf.TagListOffset = XBF.ExpectedTreeStructureOffset + l);

            XBFmap
                .WithCollectionOf(x => x.TagList)
                .WithSerializationOrderOf(11) //after taglist offset
                .AtOffset(i => i.TagListOffset)
                .WithCountOf(i => i.TagListCount)
                .WithNullTerminator()
                .AfterSerializing((xbf, l) => xbf.AttributeListOffset = xbf.TagListOffset + l);
            XBFmap
                .WithCollectionOf(x => x.AttributeList)
                .WithSerializationOrderOf(21) //after attributelist offset
                .AtOffset(i => i.AttributeListOffset)
                .WithCountOf(i => i.AttributeListCount)
                .WithNullTerminator()
                .AfterSerializing((xbf, l) => xbf.ValueListOffset = xbf.AttributeListOffset + l);
            XBFmap
                .WithCollectionOf(x => x.ValueList)
                .WithSerializationOrderOf(31) //after value list offset
                .AtOffset(i => i.ValueListOffset)
                .WithCountOf(i => i.ValueListCount)
                .WithNullTerminator();
        }
    }
}
