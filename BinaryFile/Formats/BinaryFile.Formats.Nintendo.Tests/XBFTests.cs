using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker;
using System.Security.Cryptography.X509Certificates;
using BinaryFile.Unpacker.Deserializers;

namespace BinaryFile.Formats.Nintendo.Tests
{
    public class XBFTests
    {
        [Fact]
        public void Read()
        {
            //TODO generate test samples once serialization is complete :)
            string path = @"C:\G\Kidou Senshi Gundam - MS Sensen 0079 (Japan)\JP\DATA\files\parameter\result_param.xbf";

            var ctx = new RootDataOffset(new DeserializerManager());

            XBF.Register(ctx.Manager);
            ctx.Manager.Register(new IntegerDeserializer());
            ctx.Manager.Register(new StringDeserializer());
            ctx.Manager.Register(new BinaryArrayDeserializer());

            ctx.Manager.TryGetMapping<XBF>(out var d);

            Assert.NotNull(d);

            var bytes = File.ReadAllBytes(path);

            var result = d.Deserialize(bytes.AsSpan(), ctx, out _);
            Assert.NotNull(result);

            var s = result.DebugView();
        }

        //TODO test serialization
        //TODO test read-write loop with no changes
        //TODO test read-modify-write loop
        //TODO test encodings! XBF does not have encoding field in XML tag, it has to be supplied externally and R79JAF seems to use both Shift-JIS and UTF-8
    }
}