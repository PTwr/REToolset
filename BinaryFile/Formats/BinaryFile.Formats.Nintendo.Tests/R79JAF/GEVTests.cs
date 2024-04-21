using BinaryDataHelper;
using BinaryFile.Formats.Nintendo.R79JAF;
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

            ByteBuffer buffer = new ByteBuffer();
            //TODO deserialization stuff
            m.Serialize(gev, buffer, ctx, out _);

            var modifiedBytes = buffer.GetData();
            File.WriteAllBytes("c:/dev/tmp/a.bin", cleanBytes);
            File.WriteAllBytes("c:/dev/tmp/b.bin", modifiedBytes);
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
    }
}
