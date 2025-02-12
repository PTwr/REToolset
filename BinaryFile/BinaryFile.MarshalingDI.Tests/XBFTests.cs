using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryFile.MarshalingDI.ComplexMarshaling;

namespace BinaryFile.MarshalingDI.Tests
{
    public class XBFTests
    {
        interface A { }
        interface B : A { }
        interface C : B { }


        [Fact]
        public void XBFHeaderRead()
        {
            return;
        }
    }
}
