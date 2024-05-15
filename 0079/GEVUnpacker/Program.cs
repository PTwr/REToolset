using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVELines;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;

namespace GEVUnpacker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var gevs = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\ace", "*.gev");

            var ctx = PrepMarshaling(out var m);

            foreach (var gev in gevs)
            {
                var g = m.Deserialize(null, null, File.ReadAllBytes(gev).AsMemory(), ctx, out _);

                var outputDir = gev.Replace(".gev", "").Replace("_clean", "_dirty");
                Directory.CreateDirectory(outputDir);

                foreach (var block in g.EVESegment.Blocks)
                {
                    var blockDir = outputDir + "/" +
                        block.EVELines.First().LineId.ToString("D4")
                        + "-" +
                        block.EVELines.Last().LineId.ToString("D4")
                        + $" ({block.EVELines.First().LineId:X4}-{block.EVELines.Last().LineId:X4})";
                    Directory.CreateDirectory(blockDir);

                    foreach (var line in block.EVELines)
                    {
                        var str = line.ToString();
                        File.WriteAllText(blockDir + $"/{line.LineId:D4} ({line.LineId:X4}).txt", str);
                    }
                }

                File.WriteAllText(outputDir + "/jumptable.txt",
                    g.EVESegment.Blocks
                        .SelectMany(i => i.EVELines)
                        .OfType<EVEJumpTable>()
                        .First()
                        .ToString()
                    );
                File.WriteAllText(outputDir + "/STR.txt",
                    string.Join(Environment.NewLine, g.STR.Select((s,n) => $"{n:X4}{Environment.NewLine}{s}"))
                    );
            }
        }
        private static IMarshalingContext PrepMarshaling(out ITypeMarshaler<GEV> m)
        {
            var store = new DefaultMarshalerStore();
            var rootCtx = new RootMarshalingContext(store);

            GEVMarshaling.Register(store);

            m = store.FindMarshaler<GEV>();

            return rootCtx;
        }
    }
}
