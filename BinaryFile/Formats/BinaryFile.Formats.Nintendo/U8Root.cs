using BinaryDataHelper;
using BinaryFile.Unpacker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo
{
    public class U8Root : IBinarySegment<U8Root>
    {
        public const int U8MagicNumber = 0x55AA382D;
        /// <summary>
        /// Parameterless constructor for deserialization
        /// do NOT remove
        /// </summary>
        public U8Root()
        {

        }

        public U8Root(U8Root parent)
        {
            Parent = parent;
        }

        public U8Root? Parent { get; protected set; }
    }

    //byte length = 4
    public class U8Node
    {
        public byte Type { get; set; }

        public bool IsFile => this.Type == 0x00;
        public bool IsDirectory => this.Type == 0x01;

        //TODO implement (U)Int24 reading
        //offset 1
        public UInt24 NameOffset { get; set; }
    }
    public class RootDirectory : U8Node { }
    public class U8FileNode : U8Node, IBinarySegment<U8Node>
    {
        public U8Node? Parent => throw new NotImplementedException();
    }
    public class U8DirectoryNode : U8Node, IBinarySegment<U8Node>
    {
        public U8Node? Parent => throw new NotImplementedException();
    }

}
