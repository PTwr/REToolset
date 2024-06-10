using BinaryDataHelper;
using BinaryFile.Formats.Nintendo;
using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System.Xml.Linq;
using System.Xml.XPath;

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
            int maxVoiceLinesPerFile = 0;
            int highestVoiceWaitSum = 0;
            string longestEvcCut = "";

            var ctx = PrepMarshaling(out var m, out var mX, out var mU8);
            foreach (var ff in Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_dirty\DATA\files\evc", "*.arc"))
            {
                var evc = mU8.Deserialize(null, null, File.ReadAllBytes(ff).AsMemory(), ctx, out _);
                var xbf = (evc["/arc/EvcScene.xbf"] as U8FileNode).File as XBFFile;
                var xbfDebug = (evc["/arc/EvcDebug.xbf"] as U8FileNode).File as XBFFile;

                var xml = xbf.ToXDocument().ToString();
                var xmlDebug = xbfDebug.ToXDocument().ToString();

                var cc = xbf.ToXDocument()
                    .XPathSelectElements("//Cut")
                    .Select(cut => cut.XPathSelectElements(".//Voice").Count())
                    .Max();

                maxVoiceLinesPerFile = int.Max(maxVoiceLinesPerFile, cc);

                int cutN = 0;
                foreach (var cut in xbf.ToXDocument().XPathSelectElements("//Cut"))
                {
                    var cccc = cut
                        .XPathSelectElements(".//VoiceWait")
                        .Select(i => int.Parse(i.Value))
                        .Sum();
                    if (cccc > highestVoiceWaitSum)
                    {
                        longestEvcCut = ff + " " + cutN;
                        highestVoiceWaitSum = cccc;
                    }
                }

                cutN++;

                Directory.CreateDirectory(Path.GetDirectoryName(ChangeOutputPath(ff)));
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

            Console.WriteLine($"Max voices per cut: {maxVoiceLinesPerFile}");
            Console.WriteLine($"Max voice wait per cut: {highestVoiceWaitSum}");
            Console.WriteLine($"Longest cut: {longestEvcCut}");
        }

        private static string ChangeOutputPath(string ff)
        {
            return ff
                .Replace("R79JAF_clean", "R79JAF_clean_unpacked")
                .Replace("R79JAF_dirty", "R79JAF_dirty_unpacked");
        }
    }
}
