﻿using BinaryDataHelper;
using BinaryFile.Unpacker;
using BinaryFile.Unpacker.Marshalers;
using ReflectionHelper;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BinaryFile.Formats.Nintendo.R79JAF
{
    /// <summary>
    /// Found in R79JAF
    /// </summary>
    public class XBF
    {
        //TODO .WithExpectedValueOf(...)
        public const int MagicNumber1 = 0x58_42_46_00; //"XBF";
        public const int MagicNumber2 = 0x03_00_80_00; //??? Constant in all .xbf files in R79JAF, might  be some kind of version
        public const int ExpectedTreeStructureOffset = 0x28;

        public int Magic1 { get; set; } = MagicNumber1;
        public int Magic2 { get; set; } = MagicNumber2;

        public int TreeStructureOffset { get; set; } = ExpectedTreeStructureOffset;

        public int TreeStructureCount { get; set; }
        public int TreeStructureLength => 4 * TreeStructureCount;

        public int TagListOffset { get; set; }
        public int TagListCount { get; set; }

        public int AttributeListOffset { get; set; }
        public int AttributeListCount { get; set; }

        public int ValueListOffset { get; set; }
        public int ValueListCount { get; set; }

        public List<XBFTreeNode> TreeStructure { get; private set; } = new List<XBFTreeNode>();

        //three series of null delimited string lists starting after Tree
        //original XBF's lists always have empty string at the begining, even if its not in use
        //TODO test if empty string its needed or is just artifact from whatever serializer was used
        //R79JAF files appear to always have empty string at start of list, even if its not actually used
        //TODO test if R79JAF can read XBF without unnecessary empty string, it might actually be part of structure instead of a item
        public DistinctList<string> TagList { get; private set; } = new DistinctList<string>([""]);
        public DistinctList<string> AttributeList { get; private set; } = new DistinctList<string>([""]);
        public DistinctList<string> ValueList { get; private set; } = new DistinctList<string>([""]);

        /// <summary>
        /// Parameterless ctor for Deserialization
        /// DO NOT REMOVE!
        /// </summary>
        public XBF()
        {

        }

        public XBF(XDocument doc)
        {
            //Local method 'cos it should never be used outside of ctor
            void RecursivelyFillFromXDoc(XElement treeElement)
            {
                var txts = treeElement.Nodes().OfType<XText>().Select(i => i.Value);
                var txt = string.Concat(txts);

                TreeStructure.Add(new XBFTreeNode(this)
                {
                    NameOrAttributeId = (short)TagList.Add(treeElement.Name.ToString()),
                    ValueId = (ushort)ValueList.Add(txt),
                });

                foreach (var elementAttribute in treeElement.Attributes())
                {
                    TreeStructure.Add(new XBFTreeNode(this)
                    {
                        NameOrAttributeId = (short)-AttributeList.Add(elementAttribute.Name.ToString()),
                        ValueId = (ushort)ValueList.Add(elementAttribute.Value),
                    });
                }

                foreach (var childElement in treeElement.Elements()) RecursivelyFillFromXDoc(childElement);

                TreeStructure.Add(new XBFTreeNode(this)
                {
                    NameOrAttributeId = (short)TagList.Add(treeElement.Name.ToString()),
                    ValueId = XBFTreeNode.ClosingTagMagic,
                });
            }

            if (doc.Root is null) throw new Exception("Oy, thats an empty XDoc you wanker! Check your inputs!");

            RecursivelyFillFromXDoc(doc.Root);
        }
        public override string ToString()
        {
            return ToXDocument().ToString();
        }
        public XDocument ToXDocument()
        {
            var doc = new XDocument();

            if (TreeStructure is null) throw new Exception($"{nameof(TreeStructure)} is null! XBF has not been properly deserialized from source file, or got malformed during processing.");

            XElement? currentNode = null;

            foreach (var node in TreeStructure)
            {
                if (node.IsClosingTag)
                {
                    currentNode = currentNode?.Parent;
                }
                else if (node.IsAttribute)
                {
                    var attr = new XAttribute(node.AttributeName, node.Value);
                    currentNode?.Add(attr);
                }
                else
                {
                    var newNode = new XElement(node.TagName, node.Value);

                    if (currentNode is null) doc.Add(newNode);
                    else currentNode?.Add(newNode);

                    currentNode = newNode;
                }
            }

            return doc;
        }

        //TODO conditional implementation switch?
        public class XBFTreeNode : IBinarySegment<XBF>
        {
            public const ushort ClosingTagMagic = 0xFFFF;

            public XBFTreeNode(XBF parent)
            {
                Parent = parent;
            }

            public string AttributeName => Parent.AttributeList![NameOrAttributeId * -1];
            public string TagName => Parent.TagList![NameOrAttributeId];
            public string Value => Parent.ValueList![ValueId];

            public short NameOrAttributeId { get; set; }
            public ushort ValueId { get; set; }

            public bool IsClosingTag => ValueId == ClosingTagMagic;
            public bool IsAttribute => NameOrAttributeId < 0;

            public XBF Parent { get; }

            public override string ToString()
            {
                if (IsAttribute) return $"__{AttributeName}={Value}";
                else if (IsClosingTag) return $"</{TagName}>";
                else return $"<{TagName}>{Value}";
            }
        }

        public static void Register(IDeserializerManager deserializerManager, ISerializerManager serializerManager)
        {
            var fileDeserializer = new FluentMarshaler<XBF>();
            var nodeDeserializer = new FluentMarshaler<XBFTreeNode>();

            nodeDeserializer.WithField<short>("NameOrAttributeId").AtOffset(0)
                .Into((i, x) => i.NameOrAttributeId = x).From((i) => i.NameOrAttributeId);
            nodeDeserializer.WithField<ushort>("ValueId").AtOffset(2)
                .Into((i, x) => i.ValueId = x).From((i) => i.ValueId);

            //static header
            fileDeserializer.WithField<int>("Magic1").AtOffset(0)
                .Into((i, x) => i.Magic1 = x)
                .From((i) => i.Magic1)
                .WithExpectedValueOf(MagicNumber1);
            fileDeserializer.WithField<int>("Magic2").AtOffset(4)
                .Into((i, x) => i.Magic2 = x)
                .From((i) => i.Magic2)
                .WithExpectedValueOf(MagicNumber2);
            fileDeserializer.WithField<int>("TreeStructureOffset").AtOffset(8)
                .Into((i, x) => i.TreeStructureOffset = x)
                .From((i) => i.TreeStructureOffset)
                .WithExpectedValueOf(ExpectedTreeStructureOffset);

            fileDeserializer.WithField<int>("TreeStructureCount")
                .AtOffset(12)
                .Into((i, x) => i.TreeStructureCount = x)
                .From((i) => i.TreeStructure.Count);

            fileDeserializer.WithField<int>("TagListOffset").AtOffset(16)
                .Into((i, x) => i.TagListOffset = x)
                .InSerializationOrder(10)
                .From((i) => ExpectedTreeStructureOffset + i.TreeStructure.Count * 4);
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
                .WithCollectionOf<XBFTreeNode>("TreeStructure")
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
                    xbf.TagListOffset = ExpectedTreeStructureOffset + l;
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