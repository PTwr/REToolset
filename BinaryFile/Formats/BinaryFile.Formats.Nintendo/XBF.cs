using BinaryFile.Unpacker;
using BinaryFile.Unpacker.Deserializers;

namespace BinaryFile.Formats.Nintendo
{
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
        public List<string>? TagList { get; private set; } = [""];
        public List<string>? AttributeList { get; private set; } = [""];
        public List<string>? ValueList { get; private set; } = [""];

        //TODO conditional implementation switch?
        public class XBFTreeNode
        {
            //TODO or just split into handy getters with nicer names?
            public short NameOrAttributeId { get; set; }
            public ushort ValueId { get; set; }

            public bool IsClosingTag => ValueId == 0xFFFF;
            public bool IsAttribute => NameOrAttributeId < 0;

            //TODO relation to Parent
            //TODO Activator requires IBinarySegment<TParent> interface to do it neatly
        }

        //TODO remove after testing, or turn into ToString? :D
        //TODO but ToString might be better to show actual XML
        public string DebugView()
        {
            if (TreeStructure is null) return "";

            string dump = "";
            int nesting = 0;

            foreach (var node in TreeStructure)
            {
                if (node.IsClosingTag) dump += Environment.NewLine;
                else if (node.IsAttribute) dump += $" {AttributeList[node.NameOrAttributeId]}={ValueList[node.ValueId]}";
                else dump += $"{TagList[node.NameOrAttributeId]}={ValueList[node.ValueId]}";
            }

            return dump;
        }

        public static void Register(IDeserializerManager manager)
        {
            var fileDeserializer = new FluentDeserializer<XBF>();
            var nodeDeserializer = new FluentDeserializer<XBFTreeNode>();

            nodeDeserializer.WithField<short>("NameOrAttributeId").AtOffset(0).Into((i, x) => i.NameOrAttributeId = x);
            nodeDeserializer.WithField<ushort>("ValueId").AtOffset(2).Into((i, x) => i.ValueId = x);

            //TODO Big/Small endinan!!!!
            fileDeserializer.WithField<int>("Magic1").AtOffset(0).Into((i, x) => i.Magic = x);
            fileDeserializer.WithField<int>("Magic2").AtOffset(4).Into((i, x) => i.Magic2 = x);
            fileDeserializer.WithField<int>("TreeStructureOffset").AtOffset(8).Into((i, x) => i.TreeStructureOffset = x);
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
                .Into((i, x) => i.TreeStructure = x.ToList());

            fileDeserializer
                .WithCollectionOf<string>("TagList")
                .AtOffset(i => i.TagListOffset)
                .WithCountOf(i => i.TagListCount)
                .Into((i, x) => i.TagList = x.ToList());
            fileDeserializer
                .WithCollectionOf<string>("AttributeList")
                .AtOffset(i => i.AttributeListOffset)
                .WithCountOf(i => i.AttributeListCount)
                .Into((i, x) => i.AttributeList = x.ToList());
            fileDeserializer
                .WithCollectionOf<string>("ValueList")
                .AtOffset(i => i.ValueListOffset)
                .WithCountOf(i => i.ValueListCount)
                .Into((i, x) => i.ValueList = x.ToList());

            manager.Register(fileDeserializer);
            manager.Register(nodeDeserializer);
        }
    }
}
