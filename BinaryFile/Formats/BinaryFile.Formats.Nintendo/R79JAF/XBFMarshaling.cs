using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo.R79JAF
{
    public static class XBFMarshaling
    {
        public static void Register(IMarshalerStore marshalerStore)
        {
            var xbfFile = new RootTypeMarshaler<XBF>();
            var xbfNode = new RootTypeMarshaler<XBF.XBFTreeNode>();

            marshalerStore.Register(xbfFile);
            marshalerStore.Register(xbfNode);

            xbfNode.WithField(x => x.NameOrAttributeId).AtOffset(0);
            xbfNode.WithField(x => x.ValueId).AtOffset(2);

            xbfFile.WithField(x => x.Magic1).AtOffset(0)
                .WithExpectedValueOf(XBF.MagicNumber1);
            xbfFile.WithField(x => x.Magic2).AtOffset(4)
                .WithExpectedValueOf(XBF.MagicNumber2);
            xbfFile.WithField(x => x.TreeStructureOffset).AtOffset(8)
                .WithExpectedValueOf(XBF.ExpectedTreeStructureOffset);

            xbfFile.WithField(x => x.TreeStructureCount).AtOffset(12)
                .From(i => i.TreeStructure.Count);

            xbfFile.WithField(x => x.TagListOffset).AtOffset(16)
                .WithSerializationOrderOf(10) //after Tree Structure
                .From((i) => XBF.ExpectedTreeStructureOffset + i.TreeStructure.Count * 4);
            xbfFile.WithField(x => x.TagListCount).AtOffset(20)
                .From((i) => i.TagList.Count);

            xbfFile.WithField(x => x.AttributeListOffset).AtOffset(24)
                .WithSerializationOrderOf(20); //after tag list
            xbfFile.WithField(x => x.AttributeListCount).AtOffset(28)
                .From((i) => i.AttributeList.Count);

            xbfFile.WithField(x => x.ValueListOffset).AtOffset(32)
                .WithSerializationOrderOf(30); //after attribute list
            xbfFile.WithField(x => x.ValueListCount).AtOffset(36)
                .From((i) => i.ValueList.Count);

            xbfFile
                .WithCollectionOf(x => x.TreeStructure)
                .WithSerializationOrderOf(1) //before list offsets
                .AtOffset(i => i.TreeStructureOffset)
                .WithCountOf(i => i.TreeStructureCount)
                .WithByteLengthOf(i => i.TreeStructureLength)
                .WithItemByteLengthOf(4)
                .From(i => i.TreeStructure)
                .AfterSerializing((xbf, l) => xbf.TagListOffset = XBF.ExpectedTreeStructureOffset + l);

            xbfFile
                .WithCollectionOf(x => x.TagList)
                .WithSerializationOrderOf(11) //after taglist offset
                .AtOffset(i => i.TagListOffset)
                .WithCountOf(i => i.TagListCount)
                .WithNullTerminator()
                .AfterSerializing((xbf, l) => xbf.AttributeListOffset = xbf.TagListOffset + l);
            xbfFile
                .WithCollectionOf(x => x.AttributeList)
                .WithSerializationOrderOf(21) //after attributelist offset
                .AtOffset(i => i.AttributeListOffset)
                .WithCountOf(i => i.AttributeListCount)
                .WithNullTerminator()
                .AfterSerializing((xbf, l) => xbf.ValueListOffset = xbf.AttributeListOffset + l);
            xbfFile
                .WithCollectionOf(x => x.ValueList)
                .WithSerializationOrderOf(31) //after value list offset
                .AtOffset(i => i.ValueListOffset)
                .WithCountOf(i => i.ValueListCount)
                .WithNullTerminator();
        }
    }
}
