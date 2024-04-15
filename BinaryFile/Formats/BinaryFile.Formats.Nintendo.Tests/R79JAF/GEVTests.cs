using BinaryDataHelper;
using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Unpacker;
using BinaryFile.Unpacker.Marshalers;
using BinaryFile.Unpacker.Metadata;
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

        private static void Prepare(out RootMarshalingContext ctx, out IDeserializer<GEV>? d, out ISerializer<GEV>? s)
        {
            var mgr = new MarshalerManager();
            ctx = new RootMarshalingContext(mgr, mgr);
            mgr.Register(EVEJumpTableBlock.PrepMarshaler());
            mgr.Register(EVEJumpTableEntry.PrepMarshaler());
            mgr.Register(GEV.PrepMarshaler());
            mgr.Register(EVEOpCode.PrepMarshaler());
            mgr.Register(EVELine.PrepMarshaler());
            //TODO Derivied types must be registered before base types
            //TODO Looking for "closest neighbour" in inheritance is a fucking mess, but picking exact match over derrived would be easy to add
            //TODO maps.TryGet<TDerived>(out map) can't return base for derived due to out breaking covariance
            //TODO make map store co(ntr)variant
            //TODO think about annotating Map with base type to do auto ordering on registration?
            mgr.Register(EVEBlock.PrepMarshaler());
            mgr.Register(EVESegment.PrepMarshaler());
            mgr.Register(new IntegerMarshaler());
            mgr.Register(new StringMarshaler());
            mgr.Register(new BinaryArrayMarshaler());

            ctx.DeserializerManager.TryGetMapping<GEV>(out d);
            ctx.SerializerManager.TryGetMapping<GEV>(out s);
        }

        [Fact]
        public void ReadWriteLoop()
        {
            var cleanBytes = File.ReadAllBytes(tr01gev_clean);

            Prepare(out var ctx, out var d, out var s);

            var gev = d.Deserialize(cleanBytes.AsSpan(), ctx, out _);

            ByteBuffer buffer = new ByteBuffer();
            //TODO deserialization stuff
            s.Serialize(gev, buffer, ctx, out _);

            var modifiedBytes = buffer.GetData();
            File.WriteAllBytes("c:/dev/tmp/a.bin", cleanBytes);
            File.WriteAllBytes("c:/dev/tmp/b.bin", modifiedBytes);
            Assert.Equal(cleanBytes, modifiedBytes);
        }
        [Fact]
        public void PatchStrings()
        {
            var cleanBytes = File.ReadAllBytes(tr01gev_clean);

            Prepare(out var ctx, out var d, out var s);

            var gev = d.Deserialize(cleanBytes.AsSpan(), ctx, out _);

            gev.STR[5] = "Let's learn the basic operations.";

            ByteBuffer buffer = new ByteBuffer();
            //TODO deserialization stuff
            s.Serialize(gev, buffer, ctx, out _);

            var modifiedBytes = buffer.GetData();
            File.WriteAllBytes("c:/dev/tmp/a.bin", cleanBytes);
            File.WriteAllBytes("c:/dev/tmp/b.bin", modifiedBytes);

            File.WriteAllBytes(tr01gev_dirty, modifiedBytes);
        }

        [Fact]
        public void AppendStrings()
        {
            var cleanBytes = File.ReadAllBytes(tr01gev_clean);

            Prepare(out var ctx, out var d, out var s);

            var gev = d.Deserialize(cleanBytes.AsSpan(), ctx, out _);

            gev.STR[5] = "Modify first text box to tell at a glance that game file was updated :)";
            gev.STR.Add("Unused string appended at the end should not break OFS references");

            ByteBuffer buffer = new ByteBuffer();
            //TODO deserialization stuff
            s.Serialize(gev, buffer, ctx, out _);

            var modifiedBytes = buffer.GetData();
            File.WriteAllBytes("c:/dev/tmp/a.bin", cleanBytes);
            File.WriteAllBytes("c:/dev/tmp/b.bin", modifiedBytes);

            File.WriteAllBytes(tr01gev_dirty, modifiedBytes);
        }

        [Fact]
        public void IndexTextBoxes()
        {
            var cleanBytes = File.ReadAllBytes(tr01gev_clean);

            Prepare(out var ctx, out var d, out var s);

            var gev = d.Deserialize(cleanBytes.AsSpan(), ctx, out _);

            var textBlocks = gev.EVESegment.Blocks
                .Where(i => i.EVELines?.Count == 3) //textboxes have three lines
                .Where(i => i.EVELines[0].Body[0].Instruction == 0x0003) //generic command start?
                .Where(i => i.EVELines[0].Body[0].Parameter == 0x0000) //generic command start?
                .Where(i => i.EVELines[0].Body[1].Instruction == 0x00C1) //load string through OFS?
                ;

            //cant index them all, half of them is mission variables
            //for (int n=0;n<gev.STR.Count;n++)
            //{
            //    gev.STR[n] = $"STR#{n} {gev.STR[n]}";
            //}

            foreach(var textBlock  in textBlocks)
            {
                var lineId = textBlock.EVELines[0].LineId;
                var ofsId = textBlock.EVELines[0].Body[1].Parameter;

                gev.STR[ofsId] = $"TB({lineId:X4} {ofsId:X4}) "+gev.STR[ofsId];
            }

            //gev.STR[5] = "Modify first text box to tell at a glance that game file was updated :)";
            //gev.STR.Add("Unused string appended at the end should not break OFS references");

            ByteBuffer buffer = new ByteBuffer();
            //TODO deserialization stuff
            s.Serialize(gev, buffer, ctx, out _);

            var modifiedBytes = buffer.GetData();
            File.WriteAllBytes("c:/dev/tmp/a.bin", cleanBytes);
            File.WriteAllBytes("c:/dev/tmp/b.bin", modifiedBytes);

            File.WriteAllBytes(tr01gev_dirty, modifiedBytes);
        }

        [Fact]
        public void AppendTextBoxes()
        {
            throw new NotImplementedException("TODO implement this test and required features :)");
            
            var cleanBytes = File.ReadAllBytes(tr01gev_clean);

            Prepare(out var ctx, out var d, out var s);

            var gev = d.Deserialize(cleanBytes.AsSpan(), ctx, out _);

            gev.STR[5] = "Modify first text box to tell at a glance that game file was updated :)";
            gev.STR.Add("Unused string appended at the end should not break OFS references");

            ByteBuffer buffer = new ByteBuffer();
            //TODO deserialization stuff
            s.Serialize(gev, buffer, ctx, out _);

            var modifiedBytes = buffer.GetData();
            File.WriteAllBytes("c:/dev/tmp/a.bin", cleanBytes);
            File.WriteAllBytes("c:/dev/tmp/b.bin", modifiedBytes);

            File.WriteAllBytes(tr01gev_dirty, modifiedBytes);
        }
    }
}
