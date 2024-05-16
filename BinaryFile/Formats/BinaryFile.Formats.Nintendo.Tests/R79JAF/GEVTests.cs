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
        string me01gev_clean = @"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\efsf\ME01.gev";
        string me01gev_dirty = @"C:\G\Wii\R79JAF_dirty\DATA\files\event\missionevent\efsf\ME01.gev";

        private static IMarshalingContext Prep(out ITypeMarshaler<GEV> m)
        {
            var store = new DefaultMarshalerStore();
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

            //switch tut022 to long playing voice file
            gev.STR[0x14] = "evz001";

            var prefetchLine = gev.EVESegment.Blocks[1].EVELines[0];
            prefetchLine.Body.InsertRange(13, [
                //does not work if not executed from prefetch block?
                    new EVEOpCode(0x00A4FFFF),
                    //new EVEOpCode(0x686F6100), //HOA
                    new EVEOpCode(0x61616100), //AAA - loading renamed copy works!
                    new EVEOpCode(0x00000000),
                    //this controls whether avatar is shown during speaking or not, 0x0000001 is ImageCutin, 0x00010000 is MSG_Window
                    //can be combined to load both
                    new EVEOpCode(0x00010001),
                ]);
            //TODO opcode insert/append with automatic length recalc?
            //Or recalc length when Body is touched?
            prefetchLine.LineLengthOpCode.HighWord += 4;

            //insert jump back to original code (first textbox)
            var textBoxLine = gev.EVESegment.Blocks.SelectMany(i => i.EVELines).Where(i => i.LineId == 0x0004).FirstOrDefault();
            var rerouteJumpId = jumpTable.AddJump(textBoxLine);

            var newLine1 = new EVELine(null)
            {
                LineStartOpCode = new EVEOpCode(0x0001, 0x0000),
                LineLengthOpCode = new EVEOpCode(3, 0x0002), //TODO Figure out what param means in line length
                Body = new List<EVEOpCode>()
                {
                    //does not work if not executed from prefetch block? breaks TR menu
                    //new EVEOpCode(0x00A4FFFF),
                    //new EVEOpCode(0x686F6100),
                    //new EVEOpCode(0x00000000),
                    //this controls whether avatar is shown during speaking or not, 0x0000001 is "cutin", 0x00010000 is avatar
                    //can be combined
                    //new EVEOpCode(0x00010001),

                    //new EVEOpCode(0x00110000), //thats just a jump to start logic execution!
                    //JumpTable is just a setup, which then goes over to prefetch block, which then executes first jump to line 0x03

                    //not necessary for quick jumpout
                    //new EVEOpCode(0x00030000),

                    //me01 voice playback - requries everything up to 0x0001FFFF?
                    //new EVEOpCode(0x00150002), //has to point to "actor" string? 0x03 in ME01 but 0x02 in TR01?
                    //new EVEOpCode(0x40000000),
                    new EVEOpCode(0x00030000),
                    new EVEOpCode(0x011B0031), //plays sound //on its own results in black screen and beeeeeeep

                    ////does not seem to be working in tr01, no crash but no face either.
                    ////me01 faces are probably loaded on start from resources, right after mech list
                    ////ALN have slightly different bytecode and is loaded from ImageCutin (used in EVC and maybe "I'm hit" thingies?) while DNB/LLS/HOA are loaded from MsgWind
                    //new EVEOpCode(0x0000FFFF), //top rigth corner avatar??
                    ////new EVEOpCode(0x000AFFFF), //cut in
                    ////avatar, HOA
                    //new EVEOpCode(0x40A00000),
                    ////new EVEOpCode(0x686F6100), //HOA
                    //new EVEOpCode(0x61616100), //AAA
                    //new EVEOpCode(0x00000000),

                    ////new EVEOpCode(0x0001FFFF), //top right corner
                    //new EVEOpCode(0x0002FFFF), //cutin
                    //----------------
                    //cant have both cutin and avatar? :( crash when both are present
                    new EVEOpCode(0x0000FFFF), //padding? vlaue does not matter? but opcode is required, without it crash
                    new EVEOpCode(0x40A00000),
                    //new EVEOpCode(0x686F6100), //HOA
                    new EVEOpCode(0x61616100), //AAA
                    new EVEOpCode(0x00000000),

                    //cutin last for duration of AnimClr
                    new EVEOpCode(0x00020000), //cutin
                    new EVEOpCode(0x00030000),

                    //new EVEOpCode(0x00030000),
                    new EVEOpCode(0x011B0031), //vice is played again :/ can't do cutin/avatar without sound :( Maybe add empty voice to spawn cutin?
                    new EVEOpCode(0x0000, 0xFFFF), //required padding
                    new EVEOpCode(0x40A00000), //???
                    //new EVEOpCode(0x686F6100), //HOA
                    new EVEOpCode(0x61616100), //AAA
                    new EVEOpCode(0x00000000),

                    //avatar last for duration of sound
                    new EVEOpCode(0x00010000), //avatar

                    //textbox ref to str #0 (Player1)
                    //seemingly enough for textbox??
                    //non-blocking textblock that will dissapear when another block is displayed?
                    //will work nicely in chain until last textbox stays on screen?
                    //TODO check if button can close
                    //TODO try close commands to append after last voice
                    //TODO try to figure out voice playback delay, check if 0x011B is "blocking" for duration. tr01 0x0115 uses some weird loopy delay
                    new EVEOpCode(0x00C1, 0x0000), 
                    //return jump to original code
                    new EVEOpCode(0x0011, (ushort)rerouteJumpId),

                    //TODO add delay?
                },
                Terminator = new EVEOpCode(EVEOpCode.LineTerminator),
            };
            newLine1.LineLengthOpCode.HighWord = (ushort)(newLine1.Body.Count + 3);
            var newLine2 = new EVELine(null)
            {
                LineStartOpCode = new EVEOpCode(0x0001, 0x0000),
                LineLengthOpCode = new EVEOpCode(3, 0x0002), //TODO Figure out what param means in line length
                Body = new List<EVEOpCode>()
                {
                    new EVEOpCode(0x00C1, 0x0000), 
                    //return jump to original code
                    new EVEOpCode(0x0011, (ushort)rerouteJumpId),

                    //TODO add delay?
                },
                Terminator = new EVEOpCode(EVEOpCode.LineTerminator),
            };
            newLine2.LineLengthOpCode.HighWord = (ushort)(newLine1.Body.Count + 3);

            gev.EVESegment.Blocks.Add(new EVEBlock(gev.EVESegment)
            {
                EVELines = new List<EVELine>()
                {
                    newLine1,
                },
                Terminator = new EVEOpCode(EVEOpCode.BlockTerminator),
            });

            ////point old jump to appended line
            jumpTable.RerouteJump(2, newLine1);

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
