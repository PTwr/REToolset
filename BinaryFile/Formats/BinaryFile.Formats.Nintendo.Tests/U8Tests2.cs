using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryDataHelper;
using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.PrimitiveMarshaling;
using BinaryFile.Marshaling.TypeMarshaling;

namespace BinaryFile.Formats.Nintendo.Tests
{
    public class U8Tests2
    {
        static string BRAA01txt = @"C:\G\Wii\R79JAF_dirty\DATA\files\_2d\Briefing\BR_AA01_text.arc";
        static string HomeBtnEng = @"C:\G\Wii\R79JAF_dirty\DATA\files\hbm\homeBtn_ENG.arc";

        [Fact]
        public void U8ReadTest()
        {
            var bytes = File.ReadAllBytes(HomeBtnEng);
            var ctx = Prep(out var m);

            var u8 = m.Deserialize(null, null, bytes.AsMemory(), ctx, out var l);
            //var nameOffsets = u8.Nodes.Select(i => (int)i.NameOffset).ToList();

            //TODO some asserts on tree structure
        }

        [Fact]
        public void U8ReadWriteLoopTest()
        {
            var expected = File.ReadAllBytes(HomeBtnEng);
            var ctx = Prep(out var m);

            var u8 = m.Deserialize(null, null, expected.AsMemory(), ctx, out _);

            //TODO move to typemap event
            u8.RecalculateIds();

            ByteBuffer output = new ByteBuffer();
            m.Serialize(u8, output, ctx, out _);

            //TODO test somehow node id=5 th_HomeBtn_b_btry_red.brlan is saved to short? or reports incorrect byte length?

            var actual = output.GetData();

            File.WriteAllBytes(@"c:/dev/tmp/a.bin", actual);
            File.WriteAllBytes(@"c:/dev/tmp/b.bin", expected);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void U8ReadWriteReadLoopAfterChangingTest()
        {
            var expected = File.ReadAllBytes(HomeBtnEng);
            var ctx = Prep(out var m);

            var u8 = m.Deserialize(null, null, expected.AsMemory(), ctx, out _);

            var arc = (U8DirectoryNode)u8.RootNode.Children[0];
            var anim = (U8DirectoryNode)arc.Children[0];
            var filenode = anim.Children[0] as U8FileNode;
            filenode.Name = filenode.Name.ToUpper();
            anim.Name = "Animations!!!";
            (anim.Children[1] as U8FileNode).FileContent = filenode.FileContent;

            //TODO move to typemap event
            u8.RecalculateIds();

            ByteBuffer output = new ByteBuffer();
            m.Serialize(u8, output, ctx, out _);

            //TODO test somehow node id=5 th_HomeBtn_b_btry_red.brlan is saved to short? or reports incorrect byte length?

            var actual = output.GetData();

            File.WriteAllBytes(@"c:/dev/tmp/u8edittest.bin", actual);

            var u8ReadAgain = m.Deserialize(null, null, expected.AsMemory(), ctx, out _);
            //TODO Assert that modifications can be read correctly
        }

        private static IMarshalingContext Prep(out ITypeMarshaler<U8File> m)
        {
            var store = new MarshalerStore();
            var rootCtx = new RootMarshalingContext(store);

            U8Marshaling.Register(store);

            m = store.FindMarshaler<U8File>();

            Assert.NotNull(m);

            store.Register(new IntegerMarshaler());
            store.Register(new StringMarshaler());
            store.Register(new IntegerArrayMarshaler());

            return rootCtx;
        }
    }
}
