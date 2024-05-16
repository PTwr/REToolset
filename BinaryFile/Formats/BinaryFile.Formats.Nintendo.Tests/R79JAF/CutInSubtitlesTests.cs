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
    public class CutInSubtitlesTests
    {
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
        public void LoadSubtitleCutins()
        {
            var cleanBytes = File.ReadAllBytes(me01gev_clean);

            var ctx = Prep(out var m);

            var gev = m.Deserialize(null, null, cleanBytes.AsMemory(), ctx, out _);

            var prefetchLine = gev.EVESegment.Blocks[1].EVELines[0];
            prefetchLine.Body.InsertRange(prefetchLine.Body.Count-1, [
                    //cutin/avatar load
                    new EVEOpCode(0x00A4FFFF),
                    new EVEOpCode(0x65766530), //eve0
                    new EVEOpCode(0x31310000), //11
                    //load cutin only
                    new EVEOpCode(0x00000001),

                    //cutin/avatar load
                    new EVEOpCode(0x00A4FFFF),
                    new EVEOpCode(0x65766530), //eve0
                    new EVEOpCode(0x31320000), //12
                    //load cutin only
                    new EVEOpCode(0x00000001),

                    //cutin/avatar load
                    new EVEOpCode(0x00A4FFFF),
                    new EVEOpCode(0x65766530), //eve0
                    new EVEOpCode(0x31330000), //13
                    //load cutin only
                    new EVEOpCode(0x00000001),

                    //cutin/avatar load
                    new EVEOpCode(0x00A4FFFF),
                    new EVEOpCode(0x65766530), //eve0
                    new EVEOpCode(0x31340000), //14
                    //load cutin only
                    new EVEOpCode(0x00000001),

                    //cutin/avatar load
                    new EVEOpCode(0x00A4FFFF),
                    new EVEOpCode(0x65766530), //eve0
                    new EVEOpCode(0x31350000), //15
                    //load cutin only
                    new EVEOpCode(0x00000001),

                    //cutin/avatar load
                    new EVEOpCode(0x00A4FFFF),
                    new EVEOpCode(0x65766530), //eve0
                    new EVEOpCode(0x31360000), //16
                    //load cutin only
                    new EVEOpCode(0x00000001),

                    //cutin/avatar load
                    new EVEOpCode(0x00A4FFFF),
                    new EVEOpCode(0x65766530), //eve0
                    new EVEOpCode(0x31370000), //17
                    //load cutin only
                    new EVEOpCode(0x00000001),

                    //cutin/avatar load
                    new EVEOpCode(0x00A4FFFF),
                    new EVEOpCode(0x65766530), //eve0
                    new EVEOpCode(0x31380000), //18
                    //load cutin only
                    new EVEOpCode(0x00000001),

                    //cutin/avatar load
                    new EVEOpCode(0x00A4FFFF),
                    new EVEOpCode(0x65766530), //eve0
                    new EVEOpCode(0x31390000), //19
                    //load cutin only
                    new EVEOpCode(0x00000001),
                ]);
            //TODO opcode insert/append with automatic length recalc?
            //Or recalc length when Body is touched?
            prefetchLine.LineLengthOpCode.HighWord += 4 * 9;

            ByteBuffer buffer = new ByteBuffer();
            //TODO deserialization stuff
            m.Serialize(gev, buffer, ctx, out _);

            var modifiedBytes = buffer.GetData();
            File.WriteAllBytes("c:/dev/tmp/an.bin", cleanBytes);
            File.WriteAllBytes("c:/dev/tmp/bn.bin", modifiedBytes);

            File.WriteAllBytes(me01gev_dirty, modifiedBytes);
        }
    }
}
