using BinaryDataHelper;
using BinaryFile.Marshaling.Common;
using ReflectionHelper;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BinaryFile.Formats.Nintendo.R79JAF
{
    /// <summary>
    /// Found in R79JAF
    /// </summary>
    public class XBF : IBinaryFile
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

        public List<XBFTreeNode> TreeStructure { get; set; } = new List<XBFTreeNode>();

        //three series of null delimited string lists starting after Tree
        //original XBF's lists always have empty string at the begining, even if its not in use
        //TODO test if empty string its needed or is just artifact from whatever serializer was used
        //R79JAF files appear to always have empty string at start of list, even if its not actually used
        //TODO test if R79JAF can read XBF without unnecessary empty string, it might actually be part of structure instead of a item
        public DistinctList<string> TagList { get; set; } = new DistinctList<string>([""]);
        public DistinctList<string> AttributeList { get; set; } = new DistinctList<string>([""]);
        public DistinctList<string> ValueList { get; set; } = new DistinctList<string>([""]);

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
        public class XBFTreeNode
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
    }
}
