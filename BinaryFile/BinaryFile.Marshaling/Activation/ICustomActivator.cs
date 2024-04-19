using BinaryFile.Marshaling.MarshalingContext;

namespace BinaryFile.Marshaling.Activation
{
    public interface ICustomActivator<T>
    {
        delegate T ActivatorDelegate(Memory<byte> data, IMarshalingContext ctx);
        T? Activate(object? parent, Memory<byte> data, IMarshalingContext ctx);
    }
    public interface ICustomActivator<TParent, T> : ICustomActivator<T>
    {
        delegate T ChildActivatorDelegate(TParent parent, Memory<byte> data, IMarshalingContext ctx);
    }
}
