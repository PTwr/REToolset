using BinaryDataHelper;
using BinaryFile.Unpacker;
using BinaryFile.Unpacker.Marshalers;
using ReflectionHelper;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BinaryFile.Formats.Nintendo
{
    /// <summary>
    /// Found in R79JAF
    /// </summary>
    public class XBF
    {
        //TODO .WithExpectedValueOf(...)
        public const int MagicNumber = 0x58_42_46_00; //"XBF";
        public const int MagicNumber2 = 0x03_00_80_00; //??? Constant in all .xbf files in R79JAF, might  be some kind of version
        public const int ExpectedTreeStructureOffset = 0x28;

        public int Magic { get; set; } = MagicNumber;
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

        public List<XBFTreeNode>? TreeStructure { get; private set; } = new List<XBFTreeNode>();

        //three series of null delimited string lists starting after Tree
        //original XBF's lists always have empty string at the begining, even if its not in use
        //TODO test if empty string its needed or is just artifact from whatever serializer was used
        public DistinctList<string>? TagList { get; private set; } = new DistinctList<string>();
        public DistinctList<string>? AttributeList { get; private set; } = new DistinctList<string>();
        public DistinctList<string>? ValueList { get; private set; } = new DistinctList<string>();

        //TODO conditional implementation switch?
        public class XBFTreeNode : IBinarySegment<XBF>
        {
            public const ushort ClosingTagMagic = 0xFFFF;

            public XBFTreeNode(XBF parent)
            {
                this.Parent = parent;
            }

            public string AttributeName => Parent.AttributeList![NameOrAttributeId * -1];
            public string TagName => Parent.TagList![NameOrAttributeId];
            public string Value => Parent.ValueList![ValueId];

            //TODO or just split into handy getters with nicer names?
            public short NameOrAttributeId { get; set; }
            public ushort ValueId { get; set; }

            public bool IsClosingTag => ValueId == ClosingTagMagic;
            public bool IsAttribute => NameOrAttributeId < 0;

            public XBF Parent { get; }

            //TODO relation to Parent
            //TODO Activator requires IBinarySegment<TParent> interface to do it neatly
        }

        public XBF()
        {
            
        }
        public XBF(XDocument doc)
        {
            var allElementSelector = doc.XPathSelectElements("//*");
            foreach (var treeElement in allElementSelector)
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
                        NameOrAttributeId = (short)TagList.Add(elementAttribute.Name.ToString()),
                        ValueId = (ushort)ValueList.Add(elementAttribute.Value),
                    });
                }

                TreeStructure.Add(new XBFTreeNode(this)
                {
                    NameOrAttributeId = (short)TagList.Add(treeElement.Name.ToString()),
                    ValueId = XBFTreeNode.ClosingTagMagic,
                });
            }
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

        [Obsolete("Eh, fuck XmlDocument, its obsolete crap. XDocument FTW!")]
        public XmlDocument ToXml()
        {
            var doc = new XmlDocument();

            if (TreeStructure is null) throw new Exception($"{nameof(TreeStructure)} is null! XBF has not been properly deserialized from source file, or got malformed during processing.");

            XmlNode currentNode = doc;

            foreach (var node in TreeStructure)
            {
                if (node.IsClosingTag)
                {
                    currentNode = currentNode?.ParentNode ?? throw new Exception($"Malformed tree on closing tag for {node.TagName}!");
                }
                else if (node.IsAttribute)
                {
                    var attrib = doc.CreateAttribute(node.AttributeName);
                    attrib.Value = node.Value;
                    currentNode?.Attributes?.Append(attrib);
                }
                else
                {
                    var newNode = doc.CreateElement(node.TagName);
                    newNode.InnerText = node.Value;

                    currentNode?.AppendChild(newNode);
                    currentNode = newNode;
                }
            }

            return doc;
        }

        //TODO remove after testing, or turn into ToString? :D
        //TODO but ToString might be better to show actual XML
        [Obsolete("TODO replace with XML stuff")]
        public string DebugView()
        {
            if (TreeStructure is null) return "";

            string dump = "";
            string nesting = "";

            foreach (var node in TreeStructure)
            {
                if (node.IsClosingTag)
                {
                    nesting = nesting.Substring(0, nesting.Length - 1);
                    dump += $"{Environment.NewLine}{nesting}</{node.TagName}>";
                }
                else if (node.IsAttribute) dump += $" [{node.AttributeName}={node.Value}]";
                else
                {
                    dump += $"{Environment.NewLine}{nesting}<{node.TagName}>{node.Value}";
                    nesting += " ";
                }
            }

            return dump;
        }

        public static void Register(IDeserializerManager deserializerManager, ISerializerManager serializerManager)
        {
            var fileDeserializer = new FluentMarshaler<XBF>();
            var nodeDeserializer = new FluentMarshaler<XBFTreeNode>();

            nodeDeserializer.WithField<short>("NameOrAttributeId").AtOffset(0).Into((i, x) => i.NameOrAttributeId = x);
            nodeDeserializer.WithField<ushort>("ValueId").AtOffset(2).Into((i, x) => i.ValueId = x);

            fileDeserializer.WithField<int>("Magic1").AtOffset(0).Into((i, x) => i.Magic = x).WithExpectedValueOf(MagicNumber);
            fileDeserializer.WithField<int>("Magic2").AtOffset(4).Into((i, x) => i.Magic2 = x).WithExpectedValueOf(MagicNumber2);
            fileDeserializer.WithField<int>("TreeStructureOffset").AtOffset(8).Into((i, x) => i.TreeStructureOffset = x).WithExpectedValueOf(ExpectedTreeStructureOffset);
            fileDeserializer.WithField<int>("TreeStructureCount").AtOffset(12).Into((i, x) => i.TreeStructureCount = x);
            fileDeserializer.WithField<int>("TagListOffset").AtOffset(16).Into((i, x) => i.TagListOffset = x);
            fileDeserializer.WithField<int>("TagListCount").AtOffset(20).Into((i, x) => i.TagListCount = x);
            fileDeserializer.WithField<int>("AttributeListOffset").AtOffset(24).Into((i, x) => i.AttributeListOffset = x);
            fileDeserializer.WithField<int>("AttributeListCount").AtOffset(28).Into((i, x) => i.AttributeListCount = x);
            fileDeserializer.WithField<int>("ValueListOffset").AtOffset(32).Into((i, x) => i.ValueListOffset = x);
            fileDeserializer.WithField<int>("ValueListCount").AtOffset(36).Into((i, x) => i.ValueListCount = x);

            fileDeserializer
                .WithCollectionOf<XBFTreeNode>("TreeStructure")
                .AtOffset(i => i.TreeStructureOffset)
                .WithCountOf(i => i.TreeStructureCount)
                .WithLengthOf(i => i.TreeStructureLength)
                .WithItemLengthOf(4)
                .Into((i, x) => i.TreeStructure = x.ToList());

            fileDeserializer
                .WithCollectionOf<string>("TagList")
                .AtOffset(i => i.TagListOffset)
                .WithCountOf(i => i.TagListCount)
                .WithNullTerminator()
                .Into((i, x) => i.TagList = new DistinctList<string>(x));
            fileDeserializer
                .WithCollectionOf<string>("AttributeList")
                .AtOffset(i => i.AttributeListOffset)
                .WithCountOf(i => i.AttributeListCount)
                .WithNullTerminator()
                .Into((i, x) => i.AttributeList = new DistinctList<string>(x));
            fileDeserializer
                .WithCollectionOf<string>("ValueList")
                .AtOffset(i => i.ValueListOffset)
                .WithCountOf(i => i.ValueListCount)
                .WithNullTerminator()
                .Into((i, x) => i.ValueList = new DistinctList<string>(x));

            deserializerManager.Register(fileDeserializer);
            deserializerManager.Register(nodeDeserializer);
        }
    }
}
