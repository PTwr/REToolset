using BinaryDataHelper;
using BinaryFile.Unpacker;
using BinaryFile.Unpacker.Marshalers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo
{
    public class U8File
    {
        public static FluentMarshaler<U8File> PrepareMarshaler()
        {
            var marshaler = new FluentMarshaler<U8File>();

            marshaler
                .WithField<int>("Magic")
                .AtOffset(0)
                .WithExpectedValueOf(U8MagicNumber)
                .Into((root, x) => root.Magic = x)
                .From(root => U8File.U8MagicNumber);
            marshaler
                .WithField<int>("RootNodeOffset")
                .AtOffset(4)
                .WithExpectedValueOf(ExpectedRootNodeOffset)
                .Into((root, x) => root.RootNodeOffset = x)
                .From(root => U8File.ExpectedRootNodeOffset);
            marshaler
                .WithField<int>("ContentTreeDetailsLength")
                .AtOffset(8)
                .Into((root, x) => root.ContentTreeDetailsLength = x);
            marshaler
                .WithField<int>("DataOffset")
                .AtOffset(12)
                .Into((root, x) => root.DataOffset = x);
            marshaler //just an alignment to 32bit?
                .WithCollectionOf<int>("Zeros")
                .AtOffset(16)
                .WithExpectedValueOf([0, 0, 0, 0])
                .WithCountOf(4)
                .Into((root, x) => root.Zeros = x.ToArray());

            marshaler
                .WithField<int>("DataOffset")
                .AtOffset(12)
                .Into((root, x) => root.DataOffset = x);
            marshaler
                .WithField<int>("NodeListCount")
                .AtOffset(i => i.RootNodeOffset + 8)
                .Into((root, x) => root.NodeListCount = x);

            marshaler
                .WithField<U8Node>("RootNode")
                .AtOffset(i => i.RootNodeOffset)
                .Into((u8, x) => u8.RootNode = x);

            marshaler
                .WithCollectionOf<U8Node>("nodes")
                .AtOffset(i => i.RootNodeOffset)
                .WithLengthOf(i => i.RootNode.B * 12)
                .WithItemLengthOf(12)
                .Into((file, nodes) => file.Nodes = nodes.ToList());

            marshaler
                .WithCollectionOf<string>("filenames")
                .WithNullTerminator()
                .WithLengthOf(i => i.ContentTreeDetailsLength - (i.NodeListCount * 12))
                .AtOffset(i => i.RootNodeOffset + i.NodeListCount * 12)
                .Into((root, str) =>
                {
                    var ss = str.ToList();
                    root.strings = ss;
                });

            return marshaler;
        }

        public const int U8MagicNumber = 0x55_AA_38_2D;
        public const int ExpectedRootNodeOffset = 0x20; //32

        public int Magic { get; set; }
        public int RootNodeOffset { get; set; } = ExpectedRootNodeOffset;
        public int ContentTreeDetailsLength { get; set; }
        public int DataOffset { get; set; }
        public int[] Zeros { get; set; } = [0, 0, 0, 0];
        public List<string> strings { get; set; }

        //actually a field in RootNode at RootNodeOffset + 8
        public int NodeListCount { get; set; }

        public U8Node RootNode { get; set; }

        /// <summary>
        /// Parameterless constructor for deserialization
        /// do NOT remove
        /// </summary>
        public U8File()
        {

        }

        public U8File(U8FileNode parent)
        {
            Parent = parent;
        }

        public List<U8Node> Nodes { get; set; }
        public U8FileNode Parent { get; }
    }

    //byte length = 12
    public class U8Node
    {
        public static FluentMarshaler<U8Node> PrepareMarshaler()
        {
            var marshaler = new FluentMarshaler<U8Node>();

            marshaler
                .WithField<byte>("Node type")
                .AtOffset(0)
                .Into((node, x) => node.Type = x);
            marshaler
                //TODO fix int24 marshaler offset math, then switch to uint24
                .WithField<UInt24>("Name offset")
                .AtOffset(1)
                .Into((node, x) =>
                {
                    node.NameOffset = x;
                    //node.NameOffset = new UInt24(x);
                });
            marshaler
                .WithField<int>("A")
                .AtOffset(4)
                .Into((node, x) => node.A = x);
            marshaler
                .WithField<int>("B")
                .AtOffset(8)
                .Into((node, x) =>
                {
                    node.B = x;
                });

            marshaler
                .WithField<string>("Name")
                .WithNullTerminator()
                .WithEncoding(Encoding.ASCII)
                .AtOffset(node =>
                {
                    var x = node.U8File.RootNodeOffset + node.U8File.NodeListCount * 12 + node.NameOffset;
                    return x;
                }, Unpacker.Metadata.OffsetRelation.Absolute) //out-of-segment lookup
                .Into((node, x) => node.Name = x);

            //TODO dynamic conditional deserialization
            marshaler
                .WithCollectionOf<U8Node>("Child nodes")
                .WithLengthOf(node =>
                {
                    if (node.IsFile) return 0;

                    if (node.IsDirectory) return node.B * 12;

                    return 0;
                });

            return marshaler;
        }

        public U8Node(U8File u8file)
        {
            U8File = u8file;
        }
        //TODO switch to U8DirectoryNode - requires marshaling support for Implementation Type selectors
        public U8Node(U8Node parentNode)
        {
            ParentNode = parentNode;
            U8File = parentNode.U8File;
        }

        public string Name { get; set; }

        public byte Type { get; set; }

        public bool IsFile => this.Type == 0x00;
        public bool IsDirectory => this.Type == 0x01;

        //TODO implement (U)Int24 reading
        //offset 1 as uint24, offset 2 as ushort
        //TODO fix uint24 deserialization offset calc
        //TODO maybe just register int24 as class with 3 byte fields in marshaling? but that would require some endianness check and flips
        public UInt24 NameOffset { get; set; }

        public int A { get; set; }
        public int B { get; set; }

        public U8File U8File { get; protected set; }
        //TODO switch to U8DirectoryNode
        public U8Node? ParentNode { get; protected set; }
        public U8Node RootNode => ParentNode?.RootNode ?? (this as U8Node) ??
            throw new Exception("Failed to traverse to Root DirectoryNode");

        public List<U8Node> Children { get; protected set; } = new List<U8Node>();
        //TODO this is disgusting :)
        public IEnumerable<U8Node> Descendants => [this, .. Children.SelectMany(x => x.Descendants)];
    }

    public class U8FileNode : U8Node
    {
        public U8FileNode(U8File root) : base(root)
        {
        }

        public U8FileNode(U8DirectoryNode parent) : base(parent)
        {
        }
    }
    public class U8DirectoryNode : U8Node
    {
        public U8DirectoryNode(U8File root) : base(root)
        {
        }

        public U8DirectoryNode(U8DirectoryNode parent) : base(parent)
        {
        }
    }

}
