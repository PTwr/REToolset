using BinaryFile.Formats.Nintendo.CLR0;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo.Tests
{
    public class CLR0Tests
    {
        public string SingleMaterial3FrameCLR0 = @"C:\G\Wii\R79JAF patch assets\cutintemplates\3frame\IMAGE_CUT_IN_00.clr0.bin";

        [Fact]
        public void HeaderTest()
        {
            var input = File.ReadAllBytes(SingleMaterial3FrameCLR0);
            var ctx = Prep(out var m);

            var clr = m.Deserialize(null, null, input.AsMemory(), ctx, out var l);

            Assert.Equal("IMAGE_CUT_IN_00", clr.FileName);
            Assert.Equal(3, clr.V3.FrameCount);
        }

        private static IMarshalingContext Prep(out ITypeMarshaler<CLR0File> m)
        {
            var store = new DefaultMarshalerStore();
            var rootCtx = new RootMarshalingContext(store);

            CLR0Marshaling.Register(store);

            m = store.FindMarshaler<CLR0File>();

            Assert.NotNull(m);

            return rootCtx;
        }
    }
}
