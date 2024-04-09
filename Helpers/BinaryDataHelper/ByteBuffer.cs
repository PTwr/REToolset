using System;
using System.Collections;
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
            ResizeToAtLeast(start + length);
            return data.AsSpan(start, length);
        }

        public void Emplace(int position, Span<byte> bytes)
        {
            var slice = this.Slice(position, bytes.Length);

            bytes.CopyTo(slice);
        }

        public void ResizeToAtLeast(int requiredLength)
        {
            if (requiredLength > data.Length)
            {
                //TODO check if old Spans still work after Resize
                Array.Resize(ref data, requiredLength);
            }
        }

        public byte[] GetData() => data;
    }
}
