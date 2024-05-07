using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryDataHelper
{
    public class ByteBuffer : IByteBuffer
    {
        byte[] data;
        int actualSize = 0;
        public ByteBuffer(int prealocateBytes = 1024 * 1024 * 16)
        {
            //prealocate memory because Array.Resize is very slow
            data = new byte[prealocateBytes];
        }

        public Span<byte> Slice(int start, int length)
        {
            ResizeToAtLeast(start + length);
            return data.AsSpan(start, length);
        }

        public Memory<byte> SliceMemory(int start, int length)
        {
            ResizeToAtLeast(start + length);
            return data.AsMemory(start, length);
        }

        public void Emplace(int position, Span<byte> bytes)
        {
            var slice = this.Slice(position, bytes.Length);

            bytes.CopyTo(slice);
        }

        public void Emplace(int position, byte b)
        {
            ResizeToAtLeast(position + 1);

            data[position] = b;
        }

        public void Emplace(int position, byte[] b)
        {
            ResizeToAtLeast(position + b.Length);

            Array.Copy(b, 0, data, position, b.Length);
        }

        public void ResizeToAtLeast(int requiredLength)
        {
            //gotta keep track of actual size due to prealocation
            if (requiredLength > actualSize)
            {
                actualSize = requiredLength;
            }

            if (requiredLength > data.Length)
            {
                Array.Resize(ref data, requiredLength);
            }
        }

        public byte[] GetData() => data[0..actualSize];
    }
}
