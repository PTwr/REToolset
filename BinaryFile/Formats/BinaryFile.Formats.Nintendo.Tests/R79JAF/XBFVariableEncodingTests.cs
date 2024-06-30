using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryFile.Marshaling.Common;
using BinaryDataHelper;

namespace BinaryFile.Formats.Nintendo.Tests.R79JAF
{
    public class XBFVariableEncodingTests
    {
        static string bootarc = @"C:\G\Wii\R79JAF_clean/DATA\files\boot\boot.arc";

        [Fact]
        public void ReadWriteLoopAllBootArcXbf()
        {
            foreach (var arcFile in Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean/DATA\files", "*.arc", SearchOption.AllDirectories))
            {
                var ctx = PrepXBFMarshaling(out var mXBF);

                var arc = ReadArcWithoutAutomatixXBFHandling(arcFile);

                //var arcdir = boot["/arc"] as U8DirectoryNode;

                foreach (var fileNode in arc.DescendantsOfType<U8FileNode>())
                {
                    if (!fileNode.Name.EndsWith(".xbf")) continue;

                    //if (fileNode.Name != "pilot_param.xbf") continue;

                    var expected = (fileNode.File as RawBinaryFile).Data;

                    var xbf = new XBFFile(fileNode);

                    if (fileNode.Name == "MsnMapInfo.xbf")
                    {
                        xbf.EncodingOverride = BinaryStringHelper.Shift_JIS;
                    }
                    //xbf.EncodingOverride = BinaryStringHelper.Windows1250;
                    //xbf.EncodingOverride = BinaryStringHelper.UTF8;
                    //xbf.EncodingOverride = BinaryStringHelper.Shift_JIS;
                    xbf = mXBF.Deserialize(xbf, null, expected.AsMemory(), ctx, out _);

                    var str = xbf.ToString();
                    //File.WriteAllText($"c:/dev/tmp/BootArc/{fileNode.Name}.txt", str);

                    var bb = new ByteBuffer();
                    mXBF.Serialize(xbf, bb, ctx, out _);

                    var actual = bb.GetData();

                    //File.WriteAllBytes("c:/dev/tmp/a.bin", expected);
                    //File.WriteAllBytes("c:/dev/tmp/b.bin", actual);

                    Assert.Equal(expected, actual);
                }
            }
        }

        static U8File ReadArcWithoutAutomatixXBFHandling(string arcFile)
        {
            var store = new DefaultMarshalerStore();
            var rootCtx = new RootMarshalingContext(store);

            U8Marshaling.Register(store);

            var mU8 = store.FindMarshaler<U8File>();

            var bootU8 = mU8.Deserialize(null, null, File.ReadAllBytes(arcFile).AsMemory(), rootCtx, out _);

            return bootU8;
        }

        private static IMarshalingContext PrepXBFMarshaling(out ITypeMarshaler<XBFFile> mXBF)
        {
            var store = new DefaultMarshalerStore();
            var rootCtx = new RootMarshalingContext(store);

            XBFMarshaling.Register(store);

            mXBF = store.FindMarshaler<XBFFile>();

            return rootCtx;
        }
    }
}
