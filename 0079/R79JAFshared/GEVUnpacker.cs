﻿using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVELines;
using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R79JAFshared
{
    public static class GEVUnpacker
    {
        public static void UnpackGev(IMarshalingContext ctx, ITypeMarshaler<GEV> m, string gev, string outputDir)
        {
            var g = m.Deserialize(null, null, File.ReadAllBytes(gev).AsMemory(), ctx, out _);

            Directory.CreateDirectory(outputDir);

            foreach (var block in g.EVESegment.Blocks)
            {
                var blockDir = outputDir + "/" +
                    block.EVELines.First().LineId.ToString("D4")
                    + "-" +
                    block.EVELines.Last().LineId.ToString("D4")
                    + $" (0x{block.EVELines.First().LineId:X4}-0x{block.EVELines.Last().LineId:X4})";
                Directory.CreateDirectory(blockDir);

                foreach (var line in block.EVELines)
                {
                    var str = line.ToString();
                    File.WriteAllText(blockDir + $"/{line.LineId:D4} (0x{line.LineId:X4}).txt", str);
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
                string.Join(Environment.NewLine, g.STR.Select((s, n) => $"{n:X4}{Environment.NewLine}{s}"))
                );
        }
    }
}