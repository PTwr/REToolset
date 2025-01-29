using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.ObjectMarshaling;
using BinaryFile.MarshalingDI.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Tests
{
    public class XBFTests
    {
        interface A { }
        interface B : A { }
        interface C : B { }

        class asdas : IParentingActivatorMarshaler<B>
        {
            public B Activate(IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            {
                throw new NotImplementedException();
            }

            public void RegisterChild(IActivatorMarshaler<B> childMarshaler, int order = 0)
            {
                throw new NotImplementedException();
            }

            public B TryActivate(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out bool activated)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void XBFHeaderRead()
        {

            //out
            IReadMarshaler<A> aa = null;
            IReadMarshaler<B> bb = null;
            aa = bb;
            //bb = aa;
            //in
            IWriteMarshaler<A> aa1 = null;
            IWriteMarshaler<B> bb1 = null;
            //aa1 = bb1;
            bb1 = aa1;
            IActivatorMarshaler<A> aa2 = null;
            IActivatorMarshaler<B> bb2 = null;
            IActivatorMarshaler<C> cc2 = null;
            aa2 = bb2;
            //bb2 = aa2;
            //A aaaaa = bb2.Activate(null, out _, null, null);

            //var asdas = new asdas();
            //asdas.RegisterChild(cc2);
            //asdas.RegisterChild(bb2);

            //aa2 = asdas;
            //bb2 = asdas;
            //cc2 = asdas;

            byte[] binary = [
                0x58, 0x42, 0x46, 0x00,
                0x03, 0x00, 0x80, 0x00,
                ];

            IDataBuffer dataBuffer = new DefaultDataBuffer(binary, true);
            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalerStore marshalerStore = null;

            //TODO move custom activators to separate objects? IActivator<TInterface>(priority, Func<(obj, bool)>) ??

            var xbfdescriptorbuilder = new ObjectDescriptorBuilder<XBFFile>();

            xbfdescriptorbuilder.WithTag("XBFFile");

            var magicField1builder = xbfdescriptorbuilder
                .WithField<int>()
                .AtOffset(0)
                .WithTag("Magic1")
                .WithExpectedValueOf(XBFFile.MagicNumber1)
                .FromLamda(x=>x.Magic1);

            var magicField2builder = xbfdescriptorbuilder
                .WithField<int>()
                .AtOffset(4)
                .WithTag("Magic2")
                .WithExpectedValueOf(XBFFile.MagicNumber2)
                .FromLamda(x => x.Magic2);

            XBFFile xbf = new XBFFile();
            xbf.Magic1 = 0; xbf.Magic2 = 1;

            IFieldDescriptor<XBFFile> magicField1desc = null;

            magicField1desc.Read(marshalerStore, xbf, dataBuffer, out var bytesRead, offsetStack);

            Assert.Equal(XBFFile.MagicNumber1, xbf.Magic1);
            Assert.Equal(XBFFile.MagicNumber2, xbf.Magic2);
        }
    }
}
