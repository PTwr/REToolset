using BinaryFile.Unpacker.Marshalers;
using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            var mgr = new MarshalerManager();
            var ctx = new RootMarshalingContext(mgr, mgr);
            mgr.Register(U8File.PrepareMarshaler());
            mgr.Register(U8Node.PrepareMarshaler());
            mgr.Register(new IntegerMarshaler());
            mgr.Register(new StringMarshaler());
            mgr.Register(new BinaryArrayMarshaler());

            ctx.DeserializerManager.TryGetMapping<U8File>(out var d);

            var u8 = d.Deserialize(bytes.AsSpan(), ctx, out var l);
        }
    }
}
