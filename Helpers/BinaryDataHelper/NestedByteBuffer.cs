namespace BinaryDataHelper
{
    public class NestedByteBuffer : IByteBuffer
    {
        private readonly int offset;
        private readonly IByteBuffer parent;

        public NestedByteBuffer(int offset, IByteBuffer parent)
        {
            this.offset = offset;
            this.parent = parent;
        }

        public void Emplace(int position, byte b) => parent.Emplace(position + offset, b);

        public void Emplace(int position, byte[] b) => parent.Emplace(position + offset, b);

        public void Emplace(int position, Span<byte> bytes) => parent.Emplace(position + offset, bytes);

        public byte[] GetData() => throw new NotImplementedException("Not supported in NestedByteBuffer, call it on root ByteBuffer");

        public void ResizeToAtLeast(int requiredLength) => parent.ResizeToAtLeast(requiredLength + offset);

        public Span<byte> Slice(int start, int length) => parent.Slice(start + offset, length);

        public Memory<byte> SliceMemory(int start, int length) => parent.SliceMemory(start + offset, length);
    }
}
