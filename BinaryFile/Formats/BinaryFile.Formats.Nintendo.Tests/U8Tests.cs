using BinaryFile.Unpacker.Marshalers;
using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryDataHelper;

namespace BinaryFile.Formats.Nintendo.Tests
{
    public class U8Tests
    {
        static string BRAA01txt = @"C:\G\Wii\R79JAF_dirty\DATA\files\_2d\Briefing\BR_AA01_text.arc";
        static string HomeBtnEng = @"C:\G\Wii\R79JAF_dirty\DATA\files\hbm\homeBtn_ENG.arc";

        [Fact]
        public void U8ReadTest()
        {
            var bytes = File.ReadAllBytes(HomeBtnEng);
            Prepare(out var ctx, out var d, out _);

            var u8 = d.Deserialize(bytes.AsSpan(), ctx, out var l);
            //var nameOffsets = u8.Nodes.Select(i => (int)i.NameOffset).ToList();

            //TODO some asserts on tree structure
        }

        [Fact]
        public void U8ReadWriteLoopTest()
        {
            var expected = File.ReadAllBytes(HomeBtnEng);
            Prepare(out var ctx, out var d, out var s);

            var u8 = d.Deserialize(expected.AsSpan(), ctx, out _);

            ByteBuffer output = new ByteBuffer();
            s.Serialize(u8, output, ctx, out _);

            var actual = output.GetData();

            File.WriteAllBytes(@"c:/dev/tmp/u8writetest.bin", actual);

            Assert.Equal(expected, actual);

            throw new NotImplementedException();
        }

        private static void Prepare(out RootMarshalingContext ctx, out IDeserializer<U8File>? d, out ISerializer<U8File>? s)
        {
            var mgr = new MarshalerManager();
            ctx = new RootMarshalingContext(mgr, mgr);
            mgr.Register(U8File.PrepareMarshaler());
            mgr.Register(U8FileNode.PrepareFileNodeMarshaler());
            mgr.Register(U8DirectoryNode.PrepareDirectoryNodeMarshaler());
            mgr.Register(U8Node.PrepareMarshaler());
            mgr.Register(new IntegerMarshaler());
            mgr.Register(new StringMarshaler());
            mgr.Register(new BinaryArrayMarshaler());

            ctx.DeserializerManager.TryGetMapping<U8File>(out d);
            ctx.SerializerManager.TryGetMapping<U8File>(out s);
        }
    }
}
