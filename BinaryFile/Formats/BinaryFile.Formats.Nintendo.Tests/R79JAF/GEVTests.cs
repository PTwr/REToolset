using BinaryDataHelper;
using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVELines;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.PrimitiveMarshaling;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo.Tests.R79JAF
{
    public class GEVTests
    {
        string tr01gev_clean = @"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\other\TR01.gev";
        string tr01gev_dirty = @"C:\G\Wii\R79JAF_dirty\DATA\files\event\missionevent\other\TR01.gev";

        private static IMarshalingContext Prep(out ITypeMarshaler<GEV> m)
        {
            var store = new MarshalerStore();
            var rootCtx = new RootMarshalingContext(store);

            GEVMarshaling.Register(store);

            m = store.FindMarshaler<GEV>();

            Assert.NotNull(m);

            store.Register(new IntegerMarshaler());
            store.Register(new StringMarshaler());
            store.Register(new IntegerArrayMarshaler());

            return rootCtx;
        }

        [Fact]
        public void ReadWriteLoop()
        {
            var cleanBytes = File.ReadAllBytes(tr01gev_clean);

            var ctx = Prep(out var m);

            var gev = m.Deserialize(null, null, cleanBytes.AsMemory(), ctx, out _);

            var voices =
                gev.STR.Select((s, n) => new { s, n })
                //.Where(x => x.s.StartsWith("eve") || x.s.StartsWith("bng"))
                .Select(x => $"{x.n:X2} {x.s}")
                .ToList();

            var v = string.Join(Environment.NewLine, voices);

            var jumpTableStr = gev.EVESegment.Blocks[0].EVELines[0].ToString();

            ByteBuffer buffer = new ByteBuffer();
            //TODO deserialization stuff
            m.Serialize(gev, buffer, ctx, out _);

            var modifiedBytes = buffer.GetData();
            File.WriteAllBytes("c:/dev/tmp/an.bin", cleanBytes);
            File.WriteAllBytes("c:/dev/tmp/bn.bin", modifiedBytes);
            Assert.Equal(cleanBytes, modifiedBytes);
        }

        [Fact]
        public void PatchStrings()
        {
            var cleanBytes = File.ReadAllBytes(tr01gev_clean);

            var ctx = Prep(out var m);

            var gev = m.Deserialize(null, null, cleanBytes.AsMemory(), ctx, out _);

            gev.STR[5] = "Let's learn the basic operations.";

            ByteBuffer buffer = new ByteBuffer();
            //TODO deserialization stuff
            m.Serialize(gev, buffer, ctx, out _);

            var modifiedBytes = buffer.GetData();
            File.WriteAllBytes("c:/dev/tmp/an.bin", cleanBytes);
            File.WriteAllBytes("c:/dev/tmp/bn.bin", modifiedBytes);

            File.WriteAllBytes(tr01gev_dirty, modifiedBytes);
        }

        [Fact]
        public void AppendStrings()
        {
            var cleanBytes = File.ReadAllBytes(tr01gev_clean);

            var ctx = Prep(out var m);

            var gev = m.Deserialize(null, null, cleanBytes.AsMemory(), ctx, out _);

            gev.STR[5] = "Let's learn the basic operations.";
            gev.STR.Add("Unused string appended at the end should not break OFS references");

            ByteBuffer buffer = new ByteBuffer();
            //TODO deserialization stuff
            m.Serialize(gev, buffer, ctx, out _);

            var modifiedBytes = buffer.GetData();
            File.WriteAllBytes("c:/dev/tmp/an.bin", cleanBytes);
            File.WriteAllBytes("c:/dev/tmp/bn.bin", modifiedBytes);

            File.WriteAllBytes(tr01gev_dirty, modifiedBytes);
        }

        [Fact]
        public void AppendUnusedJump()
        {
            var cleanBytes = File.ReadAllBytes(tr01gev_clean);

            var ctx = Prep(out var m);

            var gev = m.Deserialize(null, null, cleanBytes.AsMemory(), ctx, out _);

            var jumpTable = gev.EVESegment.Blocks.SelectMany(i => i.EVELines).OfType<EVEJumpTable>().FirstOrDefault();

            var jumpId = jumpTable.AddJump(jumpTable);

            ByteBuffer buffer = new ByteBuffer();
            //TODO deserialization stuff
            m.Serialize(gev, buffer, ctx, out _);

            var modifiedBytes = buffer.GetData();
            File.WriteAllBytes("c:/dev/tmp/an.bin", cleanBytes);
            File.WriteAllBytes("c:/dev/tmp/bn.bin", modifiedBytes);

            File.WriteAllBytes(tr01gev_dirty, modifiedBytes);
        }

        [Fact]
        public void AppendUnusedLine()
        {
            var cleanBytes = File.ReadAllBytes(tr01gev_clean);

            var ctx = Prep(out var m);

            var gev = m.Deserialize(null, null, cleanBytes.AsMemory(), ctx, out _);

            var jumpTable = gev.EVESegment.Blocks.SelectMany(i => i.EVELines).OfType<EVEJumpTable>().FirstOrDefault();

            gev.EVESegment.Blocks.Add(new EVEBlock(gev.EVESegment)
            {
                EVELines = new List<EVELine>()
                {
                    new EVELine(null)
                    {
                        LineStartOpCode = new EVEOpCode(0x0001, 0x0000),
                        LineLengthOpCode = new EVEOpCode(3+1, 0x0002), //TODO Figure out what param means in line length
                        Body = new List<EVEOpCode>()
                        {
                            //jump #2 to line #4
                            new EVEOpCode(0x00110002),
                        },
                        Terminator = new EVEOpCode(EVEOpCode.LineTerminator),
                    },
                },
                Terminator = new EVEOpCode(EVEOpCode.BlockTerminator),
            });

            ByteBuffer buffer = new ByteBuffer();
            //TODO deserialization stuff
            m.Serialize(gev, buffer, ctx, out _);

            var modifiedBytes = buffer.GetData();
            File.WriteAllBytes("c:/dev/tmp/an.bin", cleanBytes);
            File.WriteAllBytes("c:/dev/tmp/bn.bin", modifiedBytes);

            File.WriteAllBytes(tr01gev_dirty, modifiedBytes);
        }

        [Fact]
        public void AppendRerouteLine()
        {
            var cleanBytes = File.ReadAllBytes(tr01gev_clean);

            var ctx = Prep(out var m);

            var gev = m.Deserialize(null, null, cleanBytes.AsMemory(), ctx, out _);

            var jumpTable = gev.EVESegment.Blocks.SelectMany(i => i.EVELines).OfType<EVEJumpTable>().FirstOrDefault();

            //insert jump back to original code (first textbox)
            var textBoxLine = gev.EVESegment.Blocks.SelectMany(i => i.EVELines).Where(i => i.LineId == 0x0004).FirstOrDefault();
            var rerouteJumpId = jumpTable.AddJump(textBoxLine);

            gev.EVESegment.Blocks.Add(new EVEBlock(gev.EVESegment)
            {
                EVELines = new List<EVELine>()
                {
                    new EVELine(null)
                    {
                        LineStartOpCode = new EVEOpCode(0x0001, 0x0000),
                        LineLengthOpCode = new EVEOpCode(3+2, 0x0002), //TODO Figure out what param means in line length
                        Body = new List<EVEOpCode>()
                        {
                            //not necessary for quick jumpout
                            new EVEOpCode(0x00030000),
                            //return jump to original code
                            new EVEOpCode(0x0011, (ushort)rerouteJumpId),
                        },
                        Terminator = new EVEOpCode(EVEOpCode.LineTerminator),
                    },
                },
                Terminator = new EVEOpCode(EVEOpCode.BlockTerminator),
            });

            //point old jump to appended line
            jumpTable.RerouteJump(2, gev.EVESegment.Blocks.Last().EVELines.First());

            ByteBuffer buffer = new ByteBuffer();
            //TODO deserialization stuff
            m.Serialize(gev, buffer, ctx, out _);

            var modifiedBytes = buffer.GetData();
            File.WriteAllBytes("c:/dev/tmp/an.bin", cleanBytes);
            File.WriteAllBytes("c:/dev/tmp/bn.bin", modifiedBytes);

            File.WriteAllBytes(tr01gev_dirty, modifiedBytes);
        }

        [Fact]
        public void AppendTextboxRerouteLine()
        {
            var cleanBytes = File.ReadAllBytes(tr01gev_clean);

            var ctx = Prep(out var m);

            var gev = m.Deserialize(null, null, cleanBytes.AsMemory(), ctx, out _);

            var jumpTable = gev.EVESegment.Blocks.SelectMany(i => i.EVELines).OfType<EVEJumpTable>().FirstOrDefault();

            //insert jump back to original code (first textbox)
            var textBoxLine = gev.EVESegment.Blocks.SelectMany(i => i.EVELines).Where(i => i.LineId == 0x0004).FirstOrDefault();
            var rerouteJumpId = jumpTable.AddJump(textBoxLine);

            gev.EVESegment.Blocks.Add(new EVEBlock(gev.EVESegment)
            {
                EVELines = new List<EVELine>()
                {
                    new EVELine(null)
                    {
                        LineStartOpCode = new EVEOpCode(0x0001, 0x0000),
                        LineLengthOpCode = new EVEOpCode(3+2, 0x0002), //TODO Figure out what param means in line length
                        Body = new List<EVEOpCode>()
                        {
                            //not necessary for quick jumpout
                            new EVEOpCode(0x00030000),
                            //return jump to original code
                            new EVEOpCode(0x0011, (ushort)rerouteJumpId),
                        },
                        Terminator = new EVEOpCode(EVEOpCode.LineTerminator),
                    },
                },
                Terminator = new EVEOpCode(EVEOpCode.BlockTerminator),
            });

            //point old jump to appended line
            jumpTable.RerouteJump(2, gev.EVESegment.Blocks.Last().EVELines.First());

            ByteBuffer buffer = new ByteBuffer();
            //TODO deserialization stuff
            m.Serialize(gev, buffer, ctx, out _);

            var modifiedBytes = buffer.GetData();
            File.WriteAllBytes("c:/dev/tmp/an.bin", cleanBytes);
            File.WriteAllBytes("c:/dev/tmp/bn.bin", modifiedBytes);

            File.WriteAllBytes(tr01gev_dirty, modifiedBytes);
        }
    }
}
