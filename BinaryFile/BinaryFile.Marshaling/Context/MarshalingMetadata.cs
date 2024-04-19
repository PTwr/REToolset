using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.Context
{
    public class MarshalingMetadata : IMarshalingMetadata
    {
        public MarshalingMetadata()
        {
            Encoding = Encoding.ASCII;
        }

        public MarshalingMetadata(Encoding? encoding, bool? littleEndian, bool? nullTerminated, int? itemCount)
        {
            Encoding = encoding ?? Encoding.ASCII;
            LittleEndian = littleEndian ?? false;
            NullTerminated = nullTerminated ?? false;
            ItemCount = itemCount;
        }

        public Encoding Encoding { get; protected set; }

        public bool LittleEndian { get; protected set; }

        public bool NullTerminated { get; protected set; }

        public int? ItemCount { get; protected set; }
    }
}
