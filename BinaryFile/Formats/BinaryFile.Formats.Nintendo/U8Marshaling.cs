using BinaryDataHelper;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVELines;
using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.Marshaling.Activation;
using BinaryFile.Marshaling.Common;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryFile.Marshaling.Context;

namespace BinaryFile.Formats.Nintendo
{
    public static class U8Marshaling
    {
        public static void Register(DefaultMarshalerStore marshalerStore)
        {
            var u8File = marshalerStore.DeriveBinaryFile<U8File>(new CustomActivator<U8File>((data, ctx) =>
            {
                //0x55_AA_38_2D
                if (ctx.ItemSlice(data).Span.StartsWith([0x55, 0xAA, 0x38, 0x2D]))
                        return new U8File();
                return null;
            }));

            var act = new CustomActivator<U8File, U8FileNode>((parent, data, ctx) =>
            {
                //0x55_AA_38_2D
                if (ctx.ItemSlice(data).Span.StartsWith([0x55, 0xAA, 0x38, 0x2D]))
                    if (parent is null)
                        return new U8File();
                    else
                        return new U8File(parent);
                return null;
            }, -1);

            marshalerStore.ibinaryFileMap.WithCustomActivator(act);

            //TODO due to Root in generics derrived and root maps can't be assigned to same variable, which prevent soptional inheritance
            //var u8File = new RootTypeMarshaler<U8File>();
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
            u8File.WithCollectionOf<U8FileNode, IBinaryFile>("filedata", i => i.RootNode.Flattened.OfType<U8FileNode>(), deserialize: false)
                .WithSerializationOrderOf(12)
                .WithItemByteAlignment(32)
                .AtOffset(i => i.DataOffset)
                //just flatten it and pretend its not recursive :D
                //.From(i => i.RootNode.Flattened.OfType<U8FileNode>())
                .MarshalFrom((file, node) => node.File)
                .AsNestedFile()
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

            //root node is always a directory
            u8Node.WithCustomActivator(new CustomActivator<U8Node, U8File>((parent, data, ctx) =>
            {
                return new U8DirectoryNode(parent);
            }, order: int.MinValue));
            //directories have flag in node descriptor
            u8Node.WithCustomActivator(new CustomActivator<U8Node, U8DirectoryNode>((parent, data, ctx) =>
            {
                var determinator = ctx.ItemSlice(data).Span[0];
                if (determinator == 0)
                    return new U8FileNode(parent);
                if (determinator == 1)
                    return new U8DirectoryNode(parent);
                throw new Exception($"Invalid U8 node type value of {determinator}");
            }));

            var dirNode = u8Node.Derive<U8DirectoryNode>()
                .WithByteLengthOf(node => node.ChildSegmentLength + 12);
            var fileNode = u8Node.Derive<U8FileNode>()
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

            //just reading, as writing needs to update Node Length and it affects Offset of following nodes
            fileNode
                .WithField(i => i.File, serialize: false)
                .AtOffset(i => i.FileContentOffset)
                .RelativeTo(OffsetRelation.Absolute)
                .AsNestedFile()
                .WithByteLengthOf(i => i.FileContentLength);

            //TODO prevent repetition in store itself
            //if (marshalerStore.FindMarshaler<U8File>() is not null)
            //    return;
            //marshalerStore.Register(new U8FileMarshaler());
        }
    }

    //public class U8FileMarshaler : PrimitiveHierarchicalMarshaler<U8File>, ITypeMarshaler<U8File>
    //{
    //    //before fallback marshaling
    //    public int Order => 0;

    //    //TODO separate IsFor for serialize (from object type) and deserialize (from field type)?
    //    public bool IsFor(Type t)
    //    {
    //        //for RawBinaryFile or its derived classes
    //        return t.IsAssignableTo(typeof(RawBinaryFile))
    //            ||
    //            //when deserializing to interface field
    //            t == typeof(IBinaryFile);
    //    }

    //    public override U8File? Deserialize(U8File? obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
    //    {
    //        var result = new RawBinaryFile()
    //        {
    //            FileContent = ctx.ItemSlice(data).ToArray(),
    //        };
    //        fieldByteLength = result.FileContent.Length;
    //        return result;
    //    }

    //    public override void Serialize(U8File? obj, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
    //    {
    //        fieldByteLength = obj?.FileContent?.Length ?? 0;
    //        if (obj is null || obj.FileContent is null) return;

    //        data.Emplace(ctx.ItemAbsoluteOffset, obj.FileContent);
    //    }
    //}
}
