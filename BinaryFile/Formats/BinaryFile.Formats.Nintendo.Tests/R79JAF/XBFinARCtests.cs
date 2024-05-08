using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo.Tests.R79JAF
{
    public class XBFinARCtests
    {
        [Fact]
        public void CheckIfU8DeserializesNodesAsXbf()
        {
            string path = @"C:\G\Wii\R79JAF_clean\DATA\files\_2d\Briefing\BR_AA01_text.arc";

            var ctx = PrepXBFMarshaling(out var mXBF, out var mU8);

            var data = File.ReadAllBytes(path);

            var u8 = mU8.Deserialize(null, null, data.AsMemory(), ctx, out _);
            var arc = (u8.RootNode.Children[0] as U8DirectoryNode);
            u8 = (arc.Children[0] as U8FileNode).File as U8File;
            arc = u8.RootNode.Children[0] as U8DirectoryNode;
            var blocktext = (arc.Children[0] as U8FileNode).File as XBFFile;

            Assert.NotNull(blocktext);
        }

        private static IMarshalingContext PrepXBFMarshaling(out ITypeMarshaler<XBFFile> mXBF, out ITypeMarshaler<U8File> mU8)
        {
            var store = new DefaultMarshalerStore();
            var rootCtx = new RootMarshalingContext(store);

            U8Marshaling.Register(store);
            XBFMarshaling.Register(store);

            mXBF = store.FindMarshaler<XBFFile>();
            Assert.NotNull(mXBF);

            mU8 = store.FindMarshaler<U8File>();
            Assert.NotNull(mU8);

            return rootCtx;
        }
    }
}
