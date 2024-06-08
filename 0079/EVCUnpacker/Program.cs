using BinaryDataHelper;
using BinaryFile.Formats.Nintendo;
using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System.Xml.Linq;

namespace EVCUnpacker
{
    internal class Program
    {
        private static IMarshalingContext PrepMarshaling(out ITypeMarshaler<GEV> m, out ITypeMarshaler<XBFFile> mX, out ITypeMarshaler<U8File> mU)
        {
            var store = new DefaultMarshalerStore();
            var rootCtx = new RootMarshalingContext(store);

            GEVMarshaling.Register(store);
            XBFMarshaling.Register(store);
            U8Marshaling.Register(store);

            m = store.FindMarshaler<GEV>();
            mX = store.FindMarshaler<XBFFile>();
            mU = store.FindMarshaler<U8File>();

            return rootCtx;
        }
        static void Main(string[] args)
        {
            var ctx = PrepMarshaling(out var m, out var mX, out var mU8);
            foreach (var ff in Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_dirty\DATA\files\evc", "EVC_ST_123.arc"))
            {
                var evc = mU8.Deserialize(null, null, File.ReadAllBytes(ff).AsMemory(), ctx, out _);
                var xbf = (evc["/arc/EvcScene.xbf"] as U8FileNode).File as XBFFile;
                var xbfDebug = (evc["/arc/EvcDebug.xbf"] as U8FileNode).File as XBFFile;

                var xml = xbf.ToXDocument().ToString();
                var xmlDebug = xbfDebug.ToXDocument().ToString();

                File.WriteAllText(ChangeOutputPath(ff) + ".EvcScene.txt", xml);
                File.WriteAllText(ChangeOutputPath(ff) + ".EvcDebug.txt", xmlDebug);

                foreach (var node in (evc["/arc"] as U8DirectoryNode).Children.OfType<U8FileNode>())
                {
                    var cut = node.File as U8File;

                    if (cut is null) continue;

                    var cutxbf = (cut["/arc/EvcCut.xbf"] as U8FileNode).File as XBFFile;
                    xml = cutxbf.ToXDocument().ToString();

                    File.WriteAllText(ChangeOutputPath(ff) + "__" + node.Name + "__EvcCut.txt", xml);

                    //(cut["/arc/EvcCut.xbf"] as U8FileNode).File = new XBFFile(
                    //    XDocument.Load(ff.Replace("_clean", "_dirty") + "__" + node.Name + ".txt"));
                }

                //(evc["/arc/EvcScene.xbf"] as U8FileNode).File = new XBFFile(
                //    XDocument.Load(ff.Replace("_clean", "_dirty") + ".txt"));

                //var bb = new ByteBuffer();

                //mU8.Serialize(evc, bb, ctx, out _);

                //File.WriteAllBytes(ff.Replace("_clean", "_dirty"), bb.GetData());
            }
        }

        private static string ChangeOutputPath(string ff)
        {
            return ff.Replace("_clean", "_dirty");
        }
    }
}
