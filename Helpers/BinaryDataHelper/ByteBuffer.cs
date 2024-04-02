using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryDataHelper
{
    public class ByteBuffer
    {
        byte[] data = new byte[0];

        public Span<byte> Slice(int start, int length)
        {
            var requiredLength = start + length;

            if (requiredLength > data.Length)
            {
                //TODO check if old Spans still work after Resize
                Array.Resize(ref data, requiredLength);
            }

            return data.AsSpan();
        }

        public void Emplace(int position, Span<byte> bytes)
        {
            var slice = this.Slice(position, bytes.Length);

            bytes.CopyTo(slice);
        }

        public byte[] GetData() => data;
    }
}
