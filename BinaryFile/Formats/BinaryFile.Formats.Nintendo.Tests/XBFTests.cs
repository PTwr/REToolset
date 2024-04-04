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
        [Fact]
        public void Read()
        {
            //TODO generate test samples once serialization is complete :)
            string path = @"C:\G\Wii\R79JAF_clean\DATA\files\parameter\result_param.xbf";


            var mgr = new MarshalerManager();
            var ctx = new RootMarshalingContext(mgr, mgr);

            XBF.Register(ctx.DeserializerManager, ctx.SerializerManager);
            ctx.DeserializerManager.Register(new IntegerMarshaler());
            ctx.DeserializerManager.Register(new StringMarshaler());
            ctx.DeserializerManager.Register(new BinaryArrayMarshaler());

            ctx.DeserializerManager.TryGetMapping<XBF>(out var d);

            Assert.NotNull(d);

            var bytes = File.ReadAllBytes(path);

            var result = d.Deserialize(bytes.AsSpan(), ctx, out _);
            Assert.NotNull(result);

            var s = result.DebugView();

            var xml = result.ToXml();
            var xdoc = result.ToXDocument();
            var nicestr = xdoc.ToString();

            var elems = xdoc.XPathSelectElements("//*");

            foreach(var e in elems)
            {
                var txts = e.Nodes().OfType<XText>().Select(i=>i.Value);

                var txt = string.Concat(txts);

                var t = e.Name;
                var v = e.Value;

                foreach(var a in e.Attributes())
                {
                    var name = a.Name;
                    var value = a.Value;
                }
            }
        }

        //TODO test serialization
        //TODO test read-write loop with no changes
        //TODO test read-modify-write loop
        //TODO test encodings! XBF does not have encoding field in XML tag, it has to be supplied externally and R79JAF seems to use both Shift-JIS and UTF-8
    }
}