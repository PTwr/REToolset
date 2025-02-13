namespace BinaryFile.MarshalingDI.Marshaling
{
    public interface IOrderedMarshaler
    {
        int Order(MarshalingType marshalingType) => 0;
    }
    public enum MarshalingType
    {
        Activation = 0,
        Reading = 1,
        Writing = 2,
    }
}
