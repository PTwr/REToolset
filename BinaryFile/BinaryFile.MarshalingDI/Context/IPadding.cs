namespace BinaryFile.MarshalingDI.Context
{
    public interface IPadding
    {
        void Pad(int bytesRead, out int paddedBytesRead);
    }
}
