using System.Xml.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.TypeMarshaling;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.Common;
using BinaryFile.Marshaling.PrimitiveMarshaling;

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
            var ctx = PrepXBFMarshaling(out var m);

            var expected = File.ReadAllBytes(ResultParamXbfPath);

            var result = m.Deserialize(null, null, expected.AsMemory(), ctx, out _);
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

            var recreatedXbf = new XBFFile(xdoc);
            var modifiedStr = xdoc.ToString();

            //TODO add convienient entrypoint helpers to deal with all that repetetive crap
            var serializedBuffer = new BinaryDataHelper.ByteBuffer();
            m.Serialize(recreatedXbf, serializedBuffer, ctx, out var deserializedLength);
            var newBytes = serializedBuffer.GetData();

            var deserializeModified = m.Deserialize(null, null, newBytes.AsMemory(), ctx, out _);
            var finalStr = deserializeModified.ToString();

            Assert.Equal(modifiedStr, finalStr);
        }

        [Fact]
        public void ReadWriteLoopWithNoModifications()
        {
            //TODO create helper to prep ctx and managers with default marshalers
            var ctx = PrepXBFMarshaling(out var m);

            var expected = File.ReadAllBytes(ResultParamXbfPath);

            var result = m.Deserialize(null, null, expected.AsMemory(), ctx, out _);
            Assert.NotNull(result);

            var xdoc = result.ToXDocument();

            var recreatedXbf = new XBFFile(xdoc);

            //Silly assumption that deserialization and tostrings work correctly :D
            var originalStr = result.ToString();
            var recreatedStr = recreatedXbf.ToString();

            //check if XML representation is equivalent
            Assert.Equal(originalStr, recreatedStr);

            //TODO add convienient entrypoint helpers to deal with all that repetetive crap
            var serializedBuffer = new BinaryDataHelper.ByteBuffer();
            m.Serialize(recreatedXbf, serializedBuffer, ctx, out var deserializedLength);

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
            var ctx = PrepXBFMarshaling(out var m);

            var bytes = File.ReadAllBytes(ResultParamXbfPath);

            var result = m.Deserialize(null, null, bytes.AsMemory(), ctx, out _);
            Assert.NotNull(result);

            Assert.Equal(0x0028, result.TreeStructureOffset);
            Assert.Equal(0x0DF9, result.TreeStructureCount);

            Assert.Equal(0x380C, result.TagListOffset);
            Assert.Equal(0x0014, result.TagListCount);

            Assert.Equal(0x38B7, result.AttributeListOffset);
            Assert.Equal(0x0003, result.AttributeListCount);

            Assert.Equal(0x38C2, result.ValueListOffset);
            Assert.Equal(0x00AF, result.ValueListCount);

            //TODO check some fields once test sample files are created
        }

        private static IMarshalingContext PrepXBFMarshaling(out ITypeMarshaler<XBFFile> m)
        {
            var store = new MarshalerStore();
            var rootCtx = new RootMarshalingContext(store);

            XBFMarshaling.Register(store);

            m = store.FindMarshaler<XBFFile>();

            Assert.NotNull(m);

            store.Register(new IntegerMarshaler());
            store.Register(new StringMarshaler());
            store.Register(new IntegerArrayMarshaler());

            return rootCtx;
        }

        //TODO test serialization
        //TODO test read-write loop with no changes
        //TODO test read-modify-write loop
        //TODO test encodings! XBF does not have encoding field in XML tag, it has to be supplied externally and R79JAF seems to use both Shift-JIS and UTF-8
    }
}