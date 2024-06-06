using BinaryFile.Formats.Nintendo;
using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVELines;
using BinaryFile.Marshaling.Common;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;

namespace GEVUnpacker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var gevs = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent", "*.gev", SearchOption.AllDirectories);

            var ctx = PrepMarshaling(out var m, out var mX, out var mU);

            foreach (var gev in gevs)
            {
                Console.WriteLine(gev);

                R79JAFshared.GEVUnpacker.UnpackGev(ctx, m, gev, gev.Replace(".gev", "").Replace("_clean", "_dirty"));

                var rootArcPath = gev.Replace(".gev", "_ROOT.arc");
                if (File.Exists(rootArcPath))
                {
                    var rootArc = mU.Deserialize(null, null,
                        File.ReadAllBytes(rootArcPath).AsMemory(),
                        ctx, out _);

                    //var gevXbf = (rootArc[$"/{Path.GetFileNameWithoutExtension(gev).ToUpper()}_ROOT.xml"] as U8FileNode).File as XBFFile;

                    //var xdoc = gevXbf.ToXDocument();

                    //xdoc.Save(gev.Replace(".gev", ".root.xml.txt").Replace("_clean", "_dirty"));

                    var raw = ((rootArc.RootNode.Children.OfType<U8FileNode>().Where(i => i.Name.EndsWith("_ROOT.xml")).FirstOrDefault()).File as RawBinaryFile);

                    File.WriteAllBytes(gev.Replace(".gev", ".root.xml.txt").Replace("_clean", "_dirty"), raw.Data);
                }
            }
        }

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
    }
}
