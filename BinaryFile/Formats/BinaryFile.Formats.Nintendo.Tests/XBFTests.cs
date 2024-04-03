using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker;
using BinaryFile.Unpacker.Marshalers;
using System.Xml.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace BinaryFile.Formats.Nintendo.Tests
{
    public class XBFTests
    {
        //TODO generate test samples once serialization is complete :)
        const string ResultParamXbfPath = @"C:\G\Wii\R79JAF_clean\DATA\files\parameter\result_param.xbf";

        [Fact]
        public void ReadWriteLoopWithNoModifications()
        {
            //TODO create helper to prep ctx and managers with default marshalers
            PrepXBFMarshaling(out var ctx, out var d);

            Assert.NotNull(d);

            var bytes = File.ReadAllBytes(ResultParamXbfPath);

            var result = d.Deserialize(bytes.AsSpan(), ctx, out _);
            Assert.NotNull(result);

            var xdoc = result.ToXDocument();

            var recreatedXbf = new XBF(xdoc);

            //Silly assumption that deserialization and tostrings work correctly :D
            var originalStr = result.ToString();
            var recreatedStr = recreatedXbf.ToString();

            //check if XML representation is equivalent
            Assert.Equal(originalStr, recreatedStr);

            Assert.True(ctx.SerializerManager.TryGetMapping<XBF>(out var serializer));
            Assert.NotNull(serializer);

            //TODO add convienient entrypoint helpers to deal with all that repetetive crap
            var serializedBuffer = new BinaryDataHelper.ByteBuffer();
            serializer.Serialize(recreatedXbf, serializedBuffer, ctx, out var deserializedLength);

            Assert.Equal(bytes, serializedBuffer.GetData());

            //TODO contemplate useability of this value, it is needed for Collection serialization but for objects it is not feasible to guestimate reliably (eg. out-of-segment field offsets)
            Assert.Equal(bytes.Length, deserializedLength);
        }

        [Fact]
        public void Read()
        {
            PrepXBFMarshaling(out var ctx, out var d);

            Assert.NotNull(d);

            var bytes = File.ReadAllBytes(ResultParamXbfPath);

            var result = d.Deserialize(bytes.AsSpan(), ctx, out _);
            Assert.NotNull(result);

            //TODO check some fields once test sample files are created
        }

        private static void PrepXBFMarshaling(out RootMarshalingContext ctx, out IDeserializer<XBF>? d)
        {
            var mgr = new MarshalerManager();
            ctx = new RootMarshalingContext(mgr, mgr);
            XBF.Register(ctx.DeserializerManager, ctx.SerializerManager);
            ctx.DeserializerManager.Register(new IntegerMarshaler());
            ctx.DeserializerManager.Register(new StringMarshaler());
            ctx.DeserializerManager.Register(new BinaryArrayMarshaler());

            ctx.DeserializerManager.TryGetMapping<XBF>(out d);
        }

        //TODO test serialization
        //TODO test read-write loop with no changes
        //TODO test read-modify-write loop
        //TODO test encodings! XBF does not have encoding field in XML tag, it has to be supplied externally and R79JAF seems to use both Shift-JIS and UTF-8
    }
}