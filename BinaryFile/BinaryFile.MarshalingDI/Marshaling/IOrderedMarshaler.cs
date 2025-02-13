namespace BinaryFile.MarshalingDI.Marshaling
{
    public interface IOrderedMarshaler
    {
        int Order(EMarshalingType marshalingType) => 0;
    }
    [Flags]
    public enum EMarshalingType
    {
        Activation = 1,
        Reading = 2,
        Writing = 4,
    }
}
