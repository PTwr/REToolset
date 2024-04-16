using BinaryDataHelper;
using BinaryFile.Unpacker;
using BinaryFile.Unpacker.Marshalers;

namespace BinaryFile.Formats.Nintendo.R79JAF
{
    public static class XBFOldTypeMap
    {
        public static void Register(IDeserializerManager deserializerManager, ISerializerManager serializerManager)
        {
            var fileDeserializer = new FluentMarshaler<XBF>();
            var nodeDeserializer = new FluentMarshaler<XBF.XBFTreeNode>();

            nodeDeserializer.WithField<short>("NameOrAttributeId").AtOffset(0)
                .Into((i, x) => i.NameOrAttributeId = x).From((i) => i.NameOrAttributeId);
            nodeDeserializer.WithField<ushort>("ValueId").AtOffset(2)
                .Into((i, x) => i.ValueId = x).From((i) => i.ValueId);

            //static header
            fileDeserializer.WithField<int>("Magic1").AtOffset(0)
                .Into((i, x) => i.Magic1 = x)
                .From((i) => i.Magic1)
                .WithExpectedValueOf(XBF.MagicNumber1);
            fileDeserializer.WithField<int>("Magic2").AtOffset(4)
                .Into((i, x) => i.Magic2 = x)
                .From((i) => i.Magic2)
                .WithExpectedValueOf(XBF.MagicNumber2);
            fileDeserializer.WithField<int>("TreeStructureOffset").AtOffset(8)
                .Into((i, x) => i.TreeStructureOffset = x)
                .From((i) => i.TreeStructureOffset)
                .WithExpectedValueOf(XBF.ExpectedTreeStructureOffset);

            fileDeserializer.WithField<int>("TreeStructureCount")
                .AtOffset(12)
                .Into((i, x) => i.TreeStructureCount = x)
                .From((i) => i.TreeStructure.Count);

            fileDeserializer.WithField<int>("TagListOffset").AtOffset(16)
                .Into((i, x) => i.TagListOffset = x)
                .InSerializationOrder(10)
                .From((i) => XBF.ExpectedTreeStructureOffset + i.TreeStructure.Count * 4);
            fileDeserializer.WithField<int>("TagListCount").AtOffset(20)
                .Into((i, x) => i.TagListCount = x)
                .From((i) => i.TagList.Count);

            fileDeserializer.WithField<int>("AttributeListOffset").AtOffset(24)
                .InSerializationOrder(20)
                .Into((i, x) => i.AttributeListOffset = x)
                .From((i) => i.AttributeListOffset);
            fileDeserializer.WithField<int>("AttributeListCount").AtOffset(28)
                .Into((i, x) => i.AttributeListCount = x)
                .From((i) => i.AttributeList.Count);

            fileDeserializer.WithField<int>("ValueListOffset").AtOffset(32)
                .InSerializationOrder(30)
                .Into((i, x) => i.ValueListOffset = x)
                .From((i) => i.ValueListOffset);
            fileDeserializer.WithField<int>("ValueListCount").AtOffset(36)
                .Into((i, x) => i.ValueListCount = x)
                .From((i) => i.ValueList.Count);

            fileDeserializer
                .WithCollectionOf<XBF.XBFTreeNode>("TreeStructure")
                .InSerializationOrder(1) //before list offsets
                .AtOffset(i => i.TreeStructureOffset)
                .WithCountOf(i => i.TreeStructureCount)
                .WithLengthOf(i => i.TreeStructureLength)
                .WithItemLengthOf(4)
                .Into((i, x) => i.TreeStructure = x.ToList())
                .From(i => i.TreeStructure)
                .AfterSerializing((xbf, l) =>
                {
                    var expectedL = xbf.TreeStructure.Count * 4;
                    xbf.TagListOffset = XBF.ExpectedTreeStructureOffset + l;
                });

            fileDeserializer
                .WithCollectionOf<string>("TagList")
                .InSerializationOrder(11) //after taglist offset
                .AtOffset(i => i.TagListOffset)
                .WithCountOf(i => i.TagListCount)
                .WithNullTerminator()
                .Into((i, x) => i.TagList = new DistinctList<string>(x))
                .From(i => i.TagList.Data)
                .AfterSerializing((xbf, l) =>
                {
                    xbf.AttributeListOffset = xbf.TagListOffset + l;
                });
            fileDeserializer
                .WithCollectionOf<string>("AttributeList")
                .InSerializationOrder(21) //after attributelist offset
                .AtOffset(i => i.AttributeListOffset)
                .WithCountOf(i => i.AttributeListCount)
                .WithNullTerminator()
                .Into((i, x) => i.AttributeList = new DistinctList<string>(x))
                .From(i => i.AttributeList.Data)
                .AfterSerializing((xbf, l) =>
                {
                    xbf.ValueListOffset = xbf.AttributeListOffset + l;
                });
            fileDeserializer
                .WithCollectionOf<string>("ValueList")
                .InSerializationOrder(31) //after value list offset
                .AtOffset(i => i.ValueListOffset)
                .WithCountOf(i => i.ValueListCount)
                .WithNullTerminator()
                .Into((i, x) => i.ValueList = new DistinctList<string>(x))
                .From(i => i.ValueList.Data);

            deserializerManager.Register(fileDeserializer);
            deserializerManager.Register(nodeDeserializer);

            serializerManager.Register(fileDeserializer);
            serializerManager.Register(nodeDeserializer);
        }
    }
}