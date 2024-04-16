using System.Xml.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Unpacker.New.Interfaces;
using BinaryFile.Unpacker.New.Implementation;
using static System.Formats.Asn1.AsnWriter;
using BinaryFile.Unpacker.New.Implementation.PrimitiveMarshalers;
using BinaryFile.Unpacker.New;

namespace BinaryFile.Formats.Nintendo.Tests.R79JAF
{
    public class XBFTests
    {
        //TODO generate test samples once serialization is complete :)
        const string ResultParamXbfPath = @"C:\G\Wii\R79JAF_clean\DATA\files\parameter\result_param.xbf";

        [Fact]
        public void SerializeModifiedTest()
        {
            //TODO create helper to prep ctx and managers with default marshalers
            var ctx = PrepXBFMarshaling(out var d, out var s);

            Assert.NotNull(d);

            var expected = File.ReadAllBytes(ResultParamXbfPath);

            var result = d.DeserializeInto(new XBF(), expected.AsSpan(), ctx, out _);
            Assert.NotNull(result);

            //TODO maybe add methods to edit XBF model instead of roundtripping through xdoc?
            var xdoc = result.ToXDocument();

            var originalStr = result.ToString();

            int n = 0;
            var sranks = xdoc.XPathSelectElements("//*/RANK_S");
            foreach (var srank in sranks)
            {
                srank.Value = n++.ToString();
            }

            xdoc.Root.Add(new XAttribute("Foo", "Bar"));
            xdoc.Root.Add(new XElement("aaa", "bbb"));

            var recreatedXbf = new XBF(xdoc);
            var modifiedStr = xdoc.ToString();

            Assert.NotNull(s);

            //TODO add convienient entrypoint helpers to deal with all that repetetive crap
            var serializedBuffer = new BinaryDataHelper.ByteBuffer();
            s.SerializeFrom(recreatedXbf, serializedBuffer, ctx, out var deserializedLength);
            var newBytes = serializedBuffer.GetData();

            var deserializeModified = d.DeserializeInto(new XBF(), newBytes.AsSpan(), ctx, out _);
            var finalStr = deserializeModified.ToString();

            Assert.Equal(modifiedStr, finalStr);
        }

        [Fact]
        public void ReadWriteLoopWithNoModifications()
        {
            //TODO create helper to prep ctx and managers with default marshalers
            var ctx = PrepXBFMarshaling(out var d, out var s);

            Assert.NotNull(d);

            var expected = File.ReadAllBytes(ResultParamXbfPath);

            var result = d.DeserializeInto(new XBF(), expected.AsSpan(), ctx, out _);
            Assert.NotNull(result);

            var xdoc = result.ToXDocument();

            var recreatedXbf = new XBF(xdoc);

            //Silly assumption that deserialization and tostrings work correctly :D
            var originalStr = result.ToString();
            var recreatedStr = recreatedXbf.ToString();

            //check if XML representation is equivalent
            Assert.Equal(originalStr, recreatedStr);

            Assert.NotNull(s);

            //TODO add convienient entrypoint helpers to deal with all that repetetive crap
            var serializedBuffer = new BinaryDataHelper.ByteBuffer();
            s.SerializeFrom(recreatedXbf, serializedBuffer, ctx, out var deserializedLength);

            var actual = serializedBuffer.GetData();
            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected, actual);

            //TODO contemplate useability of this value, it is needed for Collection serialization but for objects it is not feasible to guestimate reliably (eg. out-of-segment field offsets)
            //Bleh, this is unreliable at best, most likely useless
            //Assert.Equal(expected.Length, deserializedLength);
        }

        [Fact]
        public void Read()
        {
            var ctx = PrepXBFMarshaling(out var d, out var s);

            Assert.NotNull(d);

            var bytes = File.ReadAllBytes(ResultParamXbfPath);

            var result = d.DeserializeInto(new XBF(), bytes.AsSpan(), ctx, out _);
            Assert.NotNull(result);

            //TODO check some fields once test sample files are created
        }

        private static IMarshalingContext PrepXBFMarshaling(out IDeserializingMarshaler<XBF, XBF> d, out ISerializingMarshaler<XBF> s)
        {
            var store = new MarshalerStore();
            var rootCtx = new MarshalingContext("root", store, null, 0, OffsetRelation.Absolute, null);

            XBFTypeMap.Register(store);

            d = store.GetDeserializatorFor<XBF>()!;
            s = store.GetSerializatorFor<XBF>()!;

            store.RegisterPrimitiveMarshaler(new IntegerMarshaler());
            store.RegisterPrimitiveMarshaler(new StringMarshaler());
            store.RegisterPrimitiveMarshaler(new IntegerArrayMarshaler());

            return rootCtx;
        }

        //TODO test serialization
        //TODO test read-write loop with no changes
        //TODO test read-modify-write loop
        //TODO test encodings! XBF does not have encoding field in XML tag, it has to be supplied externally and R79JAF seems to use both Shift-JIS and UTF-8
    }
}