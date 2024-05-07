
namespace BinaryDataHelper
{
    public interface IByteBuffer
    {
        void Emplace(int position, byte b);
        void Emplace(int position, byte[] b);
        void Emplace(int position, Span<byte> bytes);
        byte[] GetData();
        void ResizeToAtLeast(int requiredLength);
        Span<byte> Slice(int start, int length);
        Memory<byte> SliceMemory(int start, int length);
    }
}