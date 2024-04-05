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
    public class U8Root
    {
        public static FluentMarshaler<U8Root> PrepareMarshaler()
        {
            var marshaler = new FluentMarshaler<U8Root>();

            marshaler
                .WithField<int>("Magic")
                .AtOffset(0)
                .WithExpectedValueOf(U8MagicNumber)
                .Into((root, x) => root.Magic = x)
                .From(root => U8Root.U8MagicNumber);
            marshaler
                .WithField<int>("RootNodeOffset")
                .AtOffset(4)
                .WithExpectedValueOf(ExpectedRootNodeOffset)
                .Into((root, x) => root.RootNodeOffset = x)
                .From(root => U8Root.ExpectedRootNodeOffset);
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
                .WithField<U8Node>("RootNode")
                .AtOffset(i => i.RootNodeOffset)
                .Into((u8, x) => u8.RootNode = x);

            marshaler
                .WithCollectionOf<string>("filenames")
                .WithNullTerminator()
                .WithLengthOf(i => i.ContentTreeDetailsLength - (i.RootNode.B * 12))
                .AtOffset(i => i.RootNodeOffset + i.RootNode.B * 12)
                .Into((root, str) =>
                {
                    var ss = str.ToList();
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

        //actually a field in RootNode
        public int NodeListCount { get; set; }

        public U8Node RootNode { get; set; }

        /// <summary>
        /// Parameterless constructor for deserialization
        /// do NOT remove
        /// </summary>
        public U8Root()
        {

        }

        public U8Root(U8FileNode parent)
        {
            Parent = parent;
        }

        public List<U8Node> Nodes { get; protected set; }
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
                .WithField<UInt24>("Name offset")
                .AtOffset(1)
                .Into((node, x) => node.NameOffset = x);
            marshaler
                .WithField<int>("A")
                .AtOffset(4)
                .Into((node, x) => node.A = x);
            marshaler
                .WithField<int>("B")
                .AtOffset(8)
                .Into((node, x) => node.B = x);

            marshaler
                .WithField<string>("Name")
                .WithNullTerminator()
                .WithEncoding(Encoding.ASCII)
                .AtOffset(node => node.Root.RootNodeOffset + node.Root.RootNode.B * 12 + node.NameOffset)
                .Into((node, x) => node.Name = x);

            return marshaler;
        }

        public U8Node(U8Root root)
        {
            Root = root;
        }
        public U8Node(U8DirectoryNode parent)
        {
            Parent = parent;
            Root = parent.Root;
        }

        public string Name { get; set; }

        public byte Type { get; set; }

        public bool IsFile => this.Type == 0x00;
        public bool IsDirectory => this.Type == 0x01;

        //TODO implement (U)Int24 reading
        //offset 1
        public UInt24 NameOffset { get; set; }

        public int A { get; set; }
        public int B { get; set; }

        public U8Root Root { get; protected set; }
        public U8DirectoryNode? Parent { get; protected set; }
    }

    public class U8FileNode : U8Node
    {
        public U8FileNode(U8Root root) : base(root)
        {
        }

        public U8FileNode(U8DirectoryNode parent) : base(parent)
        {
        }
    }
    public class U8DirectoryNode : U8Node
    {
        public U8DirectoryNode(U8Root root) : base(root)
        {
        }

        public U8DirectoryNode(U8DirectoryNode parent) : base(parent)
        {
        }
    }

}
