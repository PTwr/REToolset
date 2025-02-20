namespace BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers
{
    public interface IFieldMarshaler<TDeclaringType>
    {
        bool IsForReading();
        bool IsForWriting();

        int ReadOrder();
        int WriteOrder();

        void ReadField();
        void WriteField();
    }
}