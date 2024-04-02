using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Tests.Serialization
{
    public class PrimitiveTypeSerializationTests
    {
        class NumbersPOCO
        {
            public byte A { get; set; }
            public byte B { get; set; }
            public sbyte C { get; set; }
            public bool D { get; set; }

            public ushort E { get; set; }
            public short F { get; set; }

            public uint G { get; set; }
            public int H { get; set; }

            public ulong I { get; set; }
            public long J { get; set; }
        }
        [Fact]
        public void SerializeNumbersTest()
        {
            var bytes = new byte[]
            {
                1, 2, 3, 4,
            };
        }
    }
}
