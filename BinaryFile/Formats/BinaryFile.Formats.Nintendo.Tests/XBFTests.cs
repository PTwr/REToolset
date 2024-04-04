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
        public void XbfToXDocAndBack()
        {
            PrepXBFMarshaling(out var ctx, out var d);

            Assert.NotNull(d);

            var bytes = File.ReadAllBytes(ResultParamXbfPath);

            var result = d.Deserialize(bytes.AsSpan(), ctx, out _);
            Assert.NotNull(result);

            var xdoc = result.ToXDocument();

            var recreatedXbf = new XBF(xdoc);

            //Silly assumption that deserialization and tostrings work correctly :D
            var originalStr = result.DebugView();
            var recreatedStr = recreatedXbf.DebugView();

            Assert.Equal(originalStr, recreatedStr);
        }

        [Fact]
        public void Read()
        {
            PrepXBFMarshaling(out var ctx, out var d);

            Assert.NotNull(d);

            var bytes = File.ReadAllBytes(ResultParamXbfPath);

            var result = d.Deserialize(bytes.AsSpan(), ctx, out _);
            Assert.NotNull(result);

            var s = result.DebugView();

            var xml = result.ToXml();
            var xdoc = result.ToXDocument();
            var nicestr = xdoc.ToString();

            var elems = xdoc.XPathSelectElements("//*");

            foreach (var e in elems)
            {
                var txts = e.Nodes().OfType<XText>().Select(i => i.Value);

                var txt = string.Concat(txts);

                var t = e.Name;
                var v = e.Value;

                foreach (var a in e.Attributes())
                {
                    var name = a.Name;
                    var value = a.Value;
                }
            }
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