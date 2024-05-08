namespace BinaryDataHelper
{
    public class NestedByteBuffer : IByteBuffer
    {
        private readonly int offset;
        private readonly IByteBuffer parent;
        public int Length { get; private set; }

        public NestedByteBuffer(int offset, IByteBuffer parent)
        {
            this.offset = offset;
            this.parent = parent;
        }

        public void Emplace(int position, byte b)
        {
            parent.Emplace(position + offset, b);
            Length = Math.Max(Length, position + 1);
        }

        public void Emplace(int position, byte[] b)
        {
            parent.Emplace(position + offset, b);
            Length = Math.Max(Length, position + b.Length);
        }

        public void Emplace(int position, Span<byte> bytes)
        {
            parent.Emplace(position + offset, bytes);
            Length = Math.Max(Length, position + bytes.Length);
        }

        public byte[] GetData() => throw new NotImplementedException("Not supported in NestedByteBuffer, call it on root ByteBuffer");

        public void ResizeToAtLeast(int requiredLength)
        {
            parent.ResizeToAtLeast(requiredLength + offset);
            Length = Math.Max(Length, requiredLength);
        }

        public Span<byte> Slice(int start, int length)
        {
            Length = Math.Max(Length, start + length);
            return parent.Slice(start + offset, length);
        }

        public Memory<byte> SliceMemory(int start, int length)
        {
            Length = Math.Max(Length, start + length);
            return parent.SliceMemory(start + offset, length);
        }
    }
}
