using BinaryDataHelper;
using BinaryFile.Marshaling.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo
{
    public class U8File : IBinaryFile, ITraversable
    {
        public const int U8MagicNumber = 0x55_AA_38_2D;
        public const int ExpectedRootNodeOffset = 0x20; //32

        public int Magic { get; set; }
        public int RootNodeOffset { get; set; } = ExpectedRootNodeOffset;
        //12 * NodeCount + string length
        //TODO has to be serialized after string section serialization updates length?
        public int ContentTreeDetailsLength { get; set; }
        public int DataOffset { get; set; }
        public UInt128 ZeroPadding { get; set; }
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

        public IEnumerable<T> ChildrenOfType<T>()
        {
            return RootNode.ChildrenOfType<T>();
        }

        public IEnumerable<T> DescendantsOfType<T>()
        {
            return RootNode.DescendantsOfType<T>();
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

        public U8FileNode Parent { get; }

        public U8Node? this[string path]
        {
            get
            {
                return RootNode[path.TrimStart('/')];
            }
        }
    }

    //byte length = 12
    public abstract class U8Node : ITraversable
    {
        //basically a count of preceeding nodes (in flat list form)
        public int Id { get; set; } = 0; //rootNode starts with id=0

        public U8Node(U8File u8file)
        {
            U8File = u8file;
            Id = 0; //rootnode
        }
        public U8Node(U8DirectoryNode parentNode)
        {
            ParentNode = parentNode;
            U8File = parentNode.U8File;
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

        public string NestedPath
        {
            get
            {
                if (U8File.Parent is null)
                    return Path;

                return U8File.Parent.NestedPath + Path;
            }
        }

        public override string ToString()
        {
            return Path;
        }

        public abstract IEnumerable<T> ChildrenOfType<T>();
        public abstract IEnumerable<T> DescendantsOfType<T>();

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
        public U8DirectoryNode? ParentNode { get; protected set; }
        public U8DirectoryNode RootNode => ParentNode?.RootNode ?? (this as U8DirectoryNode) ??
            throw new Exception("Failed to traverse to Root DirectoryNode");
    }

    public class U8FileNode : U8Node, ITraversable
    {
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

        public IBinaryFile File { get; set; }

        public override IEnumerable<T> ChildrenOfType<T>()
        {
            if (File is T x)
                return [x];

            return Enumerable.Empty<T>();
        }

        public override IEnumerable<T> DescendantsOfType<T>()
        {
            if (File is ITraversable traversable)
                if (File is T x)
                    return new T[] { x }.Concat(traversable.DescendantsOfType<T>());
                else
                    return traversable.DescendantsOfType<T>();

            if (File is T xx)
                return [xx];
            return Enumerable.Empty<T>();
        }
    }
    public class U8DirectoryNode : U8Node, ITraversable
    {

        public U8Node? this[string path]
        {
            get
            {
                var p = path.Split('/', 2);
                var c = Children.FirstOrDefault(i => i.Name == p[0]);

                if (p.Length is 2 && c is U8DirectoryNode dir)
                    return dir[p[1]];

                if (p.Length is 2 && c is U8FileNode fileNode && fileNode.File is U8File nestedArc)
                    return nestedArc[p[1]];

                return c;
            }
        }

        public U8DirectoryNode(U8File root) : base(root)
        {
        }

        public U8DirectoryNode(U8DirectoryNode parent) : base(parent)
        {
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

        public override IEnumerable<T> ChildrenOfType<T>()
        {
            return Children.OfType<T>();
        }

        public override IEnumerable<T> DescendantsOfType<T>()
        {
            var descendants = Children.SelectMany(i => i.DescendantsOfType<T>());
            if (this is T x)
                return new T[] { x }.Concat(descendants);
            return descendants;
        }
    }

}
