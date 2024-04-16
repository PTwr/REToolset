using BinaryDataHelper;
using BinaryFile.Unpacker;
using BinaryFile.Unpacker.Marshalers;
using BinaryFile.Unpacker.Metadata;
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
                .Into((root, x) => root.ContentTreeDetailsLength = x)
                //TODO get from nodecount*12 + string section length
                .InSerializationOrder(11) //after filenames recalculate this
                .From(root => root.ContentTreeDetailsLength);
            
            marshaler //just an alignment to 32bit?
                .WithCollectionOf<int>("Zeros")
                .AtOffset(16)
                .WithExpectedValueOf([0, 0, 0, 0])
                .WithCountOf(4)
                .Into((root, x) => root.Zeros = x.ToArray())
                //TODO this might as well not be serialized, .NET memory is initialized with 0's not random junk
                .From(root => [0, 0, 0, 0]);

            //TODO don't serialize? Its field in RootNode
            marshaler
                .WithField<int>("NodeListCount")
                .AtOffset(i => i.RootNodeOffset + 8)
                .Into((root, x) => root.NodeListCount = x);

            marshaler
                .WithField<U8DirectoryNode>("RootNode")
                .AtOffset(i => i.RootNodeOffset)
                .Into((u8, x) => u8.RootNode = x);

            //TODO this is just an example of reading recursive structure as flat list
            marshaler
                .WithCollectionOf<U8Node>("nodes")
                .AtOffset(i => i.RootNodeOffset)
                .WithLengthOf(i => i.RootNode.B * 12)
                .WithItemLengthOf(12)
                .WithCustomDeserializationMappingSelector((span, ctx) =>
                {
                    return SelectU8NodeTypeMap(span, ctx);
                })
                //.Into((file, node, localId, localOffset) => file.Nodes.Add(node));
                ;//.Into((file, nodes) => file.Nodes = nodes.ToList());

            //TODO separate collection item from marshaled datatype
            // With ColelctionMarshaler<TDeclaringType, TItemType, TMarshalingType>
            // current version would inherit and pass TItemType as TMarshalingType
            // or other way around? but that would be less flexible
            // limited version for convinenice will come later!
            marshaler
                .WithCollectionOf<U8Node, string>("filenames")
                .WithNullTerminator()
                .InSerializationOrder(10)
                //TODO double check, I think I saw some shift-jis filenames somewhere in R79JAF
                .WithEncoding(Encoding.ASCII)
                //.WithLengthOf(i => i.ContentTreeDetailsLength - (i.NodeListCount * 12))
                .AtOffset(i => i.RootNodeOffset + i.RootNode.Flattened.Count() * 12)
                //just flatten it and pretend its not recursive :D
                .From(i => i.RootNode.Flattened)
                .WithMarshalingValueGetter((file, node) => node.Name)
                .AfterSerializing((file, node, n, byteLength, itemOffset) =>
                {
                    //change from relative to file start to relative to string segmetn start
                    var offset = itemOffset; //relativeOffset - file.RootNodeOffset - file.RootNode.Tree.Count() * 12;
                    node.NameOffset = new UInt24(offset);
                })
                .AfterSerializing((file, byteLength) =>
                    file.ContentTreeDetailsLength = file.RootNode.Flattened.Count() * 12 + byteLength
                );

            //after strings vere serialized
            marshaler
                .WithField<int>("DataOffset")
                .AtOffset(12)
                .Into((root, x) => root.DataOffset = x)
                .InSerializationOrder(11)
                .From(root => root.DataOffset = (32 + root.ContentTreeDetailsLength).Align(32));

            marshaler
                .WithCollectionOf<U8FileNode, byte[]>("filedata")
                .InSerializationOrder(12)
                .WithItemByteAlignment(32)
                .AtOffset(i => i.DataOffset)
                //just flatten it and pretend its not recursive :D
                .From(i => i.RootNode.Flattened.OfType<U8FileNode>())
                .WithMarshalingValueGetter((file, node) => node.FileContent)
                .AfterSerializing((file, node, n, byteLength, itemOffset) =>
                {
                    var absOffset = file.DataOffset + itemOffset;
                    node.FileContentLength = byteLength;
                    node.FileContentOffset = absOffset;
                });

            //TODO recalculate Id's, recalculate U8DirectoryNode.ParentId (A)
            marshaler
                .WithCollectionOf<U8Node>("Nodes")
                .InSerializationOrder(20) //after filename and filecontent offsets are recalculated
                .AtOffset(i => i.RootNodeOffset)
                //TODO this REALLY needs to be settable on TypeMap level, and returnable as ConsumedBytes
                //TODO having to place it on fields makes it easy to forgot, creates code duplication, and potential bugs from differences if literals are used
                //TODO but fielddescriptor overwrite is still needed jsut in case
                .WithItemLengthOf(12) 
                //just flatten it and pretend its not recursive :D
                .From(i => i.RootNode.Flattened);

            return marshaler;
        }

        //TODO move all PrepareMarshaler to some kind of Factory?
        internal static IDeserializer<U8Node> SelectU8NodeTypeMap(Span<byte> span, IMarshalingContext ctx)
        {
            var itemSlice = ctx.Slice(span);
            var determinator = itemSlice[0];

            switch (determinator)
            {
                case 0:
                    if (ctx.DeserializerManager.TryGetMapping<U8FileNode>(out var dA) is false || dA is null)
                        throw new Exception($"No mapping found for U8FileNode!");
                    return dA;
                case 1:
                    if (ctx.DeserializerManager.TryGetMapping<U8DirectoryNode>(out var dB) is false || dB is null)
                        throw new Exception($"No mapping found for U8DirectoryNode!");
                    return dB;
                default:
                    throw new ArgumentException($"Unrecognized determinator value of {determinator}!");
            }
        }

        public const int U8MagicNumber = 0x55_AA_38_2D;
        public const int ExpectedRootNodeOffset = 0x20; //32

        public int Magic { get; set; }
        public int RootNodeOffset { get; set; } = ExpectedRootNodeOffset;
        //12 * NodeCount + string length
        //TODO has to be serialized after string section serialization updates length?
        public int ContentTreeDetailsLength { get; set; }
        public int DataOffset { get; set; }
        public int[] Zeros { get; set; } = [0, 0, 0, 0];
        public List<string> strings { get; set; }

        //actually a field in RootNode at RootNodeOffset + 8
        public int NodeListCount { get; set; }

        public U8DirectoryNode RootNode { get; set; }

        //TODO call from OnBeforeSerialize on Nodes?
        public void RecalculateIds()
        {
            //TODO rewrite this disgusting mess :D

            //ensure Id's are sequential
            RootNode.Flattened.Select((x, n) =>
            {
                x.Id = n;
                return x;
            }).ToList();

            RootNode.Flattened.OfType<U8DirectoryNode>().Select((x, n) =>
            {
                //both RootNode and its subdirectories have parentId = 0
                x.ParentDirectoryId = x.ParentNode?.Id ?? 0;

                x.FirstIdOutsideOfDirectory = (x.Flattened.LastOrDefault()?.Id ?? x.Id) + 1;

                return x;
            }).ToList();
        }

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

        [Obsolete("U8 has only one root element")]
        public List<U8Node> Nodes { get; set; }
        public U8FileNode Parent { get; }
    }

    //byte length = 12
    public class U8Node
    {
        //TODO option to fetch value from lambda instead of automatic deserializer
        //TODO it will allow to set scope-dependent initial values without making single-use derived classes
        //TODO if bytes and ctx is passed it will allow custom deserialization as well
        //TODO another way to achieve that might be lambda for Activation
        //TODO explicit > implicit!
        //basicalyl a count of preceeding nodes (in flat list form)
        public int Id { get; set; } = 0; //rootNode starts with id=0
        public static FluentMarshaler<U8Node> PrepareMarshaler()
        {
            var marshaler = new FluentMarshaler<U8Node>();

            marshaler
                .BeforeDeserialization((data, ctx, obj) =>
                {
                    var offset = ctx.AbsoluteOffset;
                    var relative = offset - obj.U8File.RootNodeOffset;
                    var id = relative / 12;
                    obj.Id = id;
                });

            marshaler
                .WithField<byte>("Node type")
                .AtOffset(0)
                .Into((node, x) => node.Type = x)
                .From(node => node.Type);
            marshaler
                //TODO fix int24 marshaler offset math, then switch to uint24
                .WithField<UInt24>("Name offset")
                .AtOffset(1)
                .Into((node, x) =>
                {
                    node.NameOffset = x;
                    //node.NameOffset = new UInt24(x);
                })
                .From(node => node.NameOffset);
            marshaler
                .WithField<int>("A")
                .AtOffset(4)
                .Into((node, x) => node.A = x)
                .From(node => node.A);
            marshaler
                .WithField<int>("B")
                .AtOffset(8)
                .Into((node, x) => node.B = x)
                .From(node => node.B);

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

            return marshaler;
        }

        public U8Node(U8File u8file)
        {
            U8File = u8file;
            Id = 0; //rootnode
        }
        //TODO switch to U8DirectoryNode - requires marshaling support for Implementation Type selectors
        public U8Node(U8Node parentNode)
        {
            ParentNode = parentNode;
            U8File = parentNode.U8File;
            //Id = RootNode.Tree.Count();
            //Id++;
        }

        public string Name { get; set; }

        public string Path
        {
            get
            {
                //TODO I hate this, nameless root screws path joining :D
                if (ParentNode is not null)
                    if (ParentNode == RootNode)
                        return $"/{Name}";
                    else
                        return $"{ParentNode.Path}/{Name}";
                return $"/{Name}";
            }
        }

        public override string ToString()
        {
            return Path;
        }

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
        public U8DirectoryNode RootNode => ParentNode?.RootNode ?? (this as U8DirectoryNode) ??
            throw new Exception("Failed to traverse to Root DirectoryNode");
    }

    public class U8FileNode : U8Node
    {
        public static FluentMarshaler<U8FileNode, U8Node> PrepareFileNodeMarshaler()
        {
            var marshaler = new FluentMarshaler<U8FileNode, U8Node>();

            marshaler
                .InheritsFrom(U8Node.PrepareMarshaler());

            //TODO file content
            marshaler
                .WithField<byte[]>("FileContent")
                .AtOffset(i => i.FileContentOffset, OffsetRelation.Absolute)
                .WithLengthOf(i => i.FileContentLength)
                .Into((node, x) => node.FileContent = x);

            return marshaler;
        }

        public int FileContentOffset
        {
            get => this.A;
            set => this.A = value;
        }
        public int FileContentLength
        {
            get => this.B;
            set => this.B = value;
        }

        public U8FileNode(U8File root) : base(root)
        {
        }

        public U8FileNode(U8DirectoryNode parent) : base(parent)
        {
        }

        public byte[] FileContent { get; set; }
    }
    public class U8DirectoryNode : U8Node
    {
        public U8DirectoryNode(U8File root) : base(root)
        {
        }

        public U8DirectoryNode(U8DirectoryNode parent) : base(parent)
        {
        }

        public static FluentMarshaler<U8DirectoryNode, U8Node> PrepareDirectoryNodeMarshaler()
        {
            var marshaler = new FluentMarshaler<U8DirectoryNode, U8Node>();

            marshaler
                .InheritsFrom(U8Node.PrepareMarshaler());

            //TODO nested nodes
            marshaler
                .WithCollectionOf<U8Node>("children")

                //has to be absolute, can't do ancestor relations when recursing
                .AtOffset(i =>
                {
                    var offset = i.U8File.RootNodeOffset //starting from section start
                    + i.Id * 12 //skip over preceeding nodes
                    + 12 //and this node descriptor
                    ;

                    return offset;
                }, OffsetRelation.Absolute)
                .WithLengthOf(i =>
                {
                    return i.ChildSegmentLength;
                }) //this also needs nodeid to calc child segment length
                   //.WithItemLengthOf(12)
                .WithItemLengthOf((parent, child) =>
                {
                    if (child.IsDirectory)
                    {
                        var dir = (U8DirectoryNode)child;
                        return dir.ChildSegmentLength + 12;
                    }
                    return 12;
                })
                .WithCustomDeserializationMappingSelector((span, ctx) =>
                {
                    return U8File.SelectU8NodeTypeMap(span, ctx);
                })
                //TODO this won't update parent.Id before child looks at it :/
                //TODO do it via custom activator, or just through hierarchical constructor?
                //TODO overloads with less params :)
                .Into((parent, node, marshaled, localId, localOffset) =>
                {
                    //localId is 0-based, rootNode starts with Id=0
                    //node.Id = parent.Id + localId + 1;
                    parent.Children.Add(node);
                });
            ;
            //.Into((file, nodes) => file.Children = nodes.ToList());

            return marshaler;
        }

        public bool IsRoot => Id == 0;
        public int ParentDirectoryId
        {
            get => this.A;
            set => this.A = value;
        }
        public int FirstIdOutsideOfDirectory
        {
            get => this.B;
            set => this.B = value;
        }

        public int ChildSegmentLength => (this.FirstIdOutsideOfDirectory - 1 - this.Id) * 12;

        public List<U8Node> Children { get; set; } = new List<U8Node>();

        public IEnumerable<U8Node> Flattened => [this,
            ..Children
            .OfType<U8FileNode>(),
            ..Children
            .OfType<U8DirectoryNode>()
            .SelectMany(x => x.Flattened)];
    }

}
