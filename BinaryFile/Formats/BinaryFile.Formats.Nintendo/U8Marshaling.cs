using BinaryDataHelper;
using BinaryFile.Marshaling.Activation;
using BinaryFile.Marshaling.Common;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo
{
    public static class U8Marshaling
    {
        public static void Register(IMarshalerStore marshalerStore)
        {
            var u8File = new RootTypeMarshaler<U8File>();
            var u8Node = new RootTypeMarshaler<U8Node>();

            marshalerStore.Register(u8File);
            marshalerStore.Register(u8Node);

            u8File.WithField(i => i.Magic)
                .AtOffset(0)
                .WithExpectedValueOf(U8File.U8MagicNumber);
            u8File.WithField(i => i.RootNodeOffset)
                .AtOffset(4)
                .WithExpectedValueOf(U8File.ExpectedRootNodeOffset);
            u8File.WithField(i => i.ContentTreeDetailsLength)
                .AtOffset(8)
                .WithSerializationOrderOf(11);

            u8File.WithField(i => i.ZeroPadding)
                .AtOffset(16)
                .WithExpectedValueOf(0);

            //dont serialize, its a field inside RootNode
            u8File.WithField(i => i.NodeListCount, serialize: false)
                .AtOffset(i => i.RootNodeOffset + 8);

            u8File.WithField(i => i.RootNode, serialize: false)
                .AtOffset(i => i.RootNodeOffset);

            //TODO filenames
            u8File.WithCollectionOf<U8Node, string>("filenames", i => i.RootNode.Flattened, deserialize: false)
                .WithNullTerminator()
                .WithSerializationOrderOf(10)
                //TODO double check, I think I saw some shift-jis filenames somewhere in R79JAF
                .WithEncoding(Encoding.ASCII)
                //.WithLengthOf(i => i.ContentTreeDetailsLength - (i.NodeListCount * 12))
                .AtOffset(i => i.RootNodeOffset + i.RootNode.Flattened.Count() * 12)
                //just flatten it and pretend its not recursive :D
                .From(i => i.RootNode.Flattened)
                .MarshalFrom((file, node) => node.Name)
                .AfterSerializingItem((file, node, n, byteLength, itemOffset) =>
                {
                    //change from relative to file start to relative to string segmetn start
                    var offset = itemOffset; //relativeOffset - file.RootNodeOffset - file.RootNode.Tree.Count() * 12;
                    node.NameOffset = new UInt24(offset);
                })
                .AfterSerializing((file, byteLength) =>
                    file.ContentTreeDetailsLength = file.RootNode.Flattened.Count() * 12 + byteLength
                );

            u8File.WithField(i => i.DataOffset)
                .AtOffset(12)
                .Into((root, x) => root.DataOffset = x)
                .WithSerializationOrderOf(11)
                .From(root => root.DataOffset = (32 + root.ContentTreeDetailsLength).Align(32));

            //TODO filedata
            u8File.WithCollectionOf<U8FileNode, byte[]>("filedata", i => i.RootNode.Flattened.OfType<U8FileNode>(), deserialize: false)
                .WithSerializationOrderOf(12)
                .WithItemByteAlignment(32)
                .AtOffset(i => i.DataOffset)
                //just flatten it and pretend its not recursive :D
                //.From(i => i.RootNode.Flattened.OfType<U8FileNode>())
                .MarshalFrom((file, node) => node.FileContent)
                .AfterSerializingItem((file, node, n, byteLength, itemOffset) =>
                {
                    var absOffset = file.DataOffset + itemOffset;
                    node.FileContentLength = byteLength;
                    node.FileContentOffset = absOffset;
                });

            //TODO try doing it through RootNode recurse?
            u8File.WithCollectionOf(i => i.RootNode.Flattened, deserialize: false)
                .WithSerializationOrderOf(20)
                .AtOffset(i => i.RootNodeOffset)
                .WithItemByteLengthOf(12)
                .RelativeTo(OffsetRelation.Absolute);

            //////////////////////////////////////////////////////////////

            u8Node
                .BeforeDeserialization((obj, data, ctx) =>
                {
                    var offset = ctx.ItemAbsoluteOffset;
                    var relative = offset - obj.U8File.RootNodeOffset;
                    var id = relative / 12;
                    obj.Id = id;
                });

            u8Node
                .WithField(i => i.Type)
                .AtOffset(0);
            u8Node
                .WithField(i => i.NameOffset)
                .AtOffset(1);
            u8Node
                .WithField(i => i.A)
                .AtOffset(4);
            u8Node
                .WithField(i => i.B)
                .AtOffset(8);

            u8Node
                .WithField(i => i.Name, serialize: false)
                .WithNullTerminator()
                .WithEncoding(Encoding.ASCII)
                .AtOffset(node =>
                {
                    var x = node.U8File.RootNodeOffset + node.U8File.NodeListCount * 12 + node.NameOffset;
                    return x;
                })
                .RelativeTo(OffsetRelation.Absolute); //out-of-segment lookup

            u8Node.WithByteLengthOf((node) =>
            {
                if (node is U8DirectoryNode)
                    return ((U8DirectoryNode)node).ChildSegmentLength + 12;
                return 12;
            });

            //TODO helper .ConditionalyActivate
            var nodeActivator = new CustomActivator<U8Node, U8DirectoryNode>((parent, data, ctx) =>
            {
                //conditional activation by byte pattern
                if (ctx.ItemSlice(data).Span[0] == 0)
                    return new U8FileNode(parent);
                if (ctx.ItemSlice(data).Span[0] == 1)
                    return new U8DirectoryNode(parent);

                //return execution to default activator
                return null;
            });
            var rootNodeActivator = new CustomActivator<U8Node, U8File>((parent, data, ctx) =>
            {
                return new U8DirectoryNode(parent);
            });

            u8Node.WithCustomActivator(nodeActivator);
            u8Node.WithCustomActivator(rootNodeActivator);

            var dirNode = u8Node.Derive<U8DirectoryNode>()
                .WithByteLengthOf(node => node.ChildSegmentLength + 12);
            var filNode = u8Node.Derive<U8FileNode>()
                .WithByteLengthOf(12);

            dirNode
                .WithCollectionOf(i => i.Children, serialize: false)
                .WithDeserializationOrderOf(10) //after base fields                                               
                .AtOffset(i =>
                {
                    return i.U8File.RootNodeOffset //starting from section start
                    + i.Id * 12 //skip over preceeding nodes
                    + 12; //and this node descriptor
                })
                //has to be absolute, can't do ancestor relations when recursing
                .RelativeTo(OffsetRelation.Absolute)
                .WithByteLengthOf(i => i.ChildSegmentLength);

            filNode
                .WithField(i => i.FileContent, serialize: false)
                .AtOffset(i => i.FileContentOffset)
                .RelativeTo(OffsetRelation.Absolute)
                .WithByteLengthOf(i => i.FileContentLength);
        }
    }
}
