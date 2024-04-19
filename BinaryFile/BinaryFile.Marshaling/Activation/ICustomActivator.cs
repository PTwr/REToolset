using BinaryFile.Marshaling.MarshalingContext;

namespace BinaryFile.Marshaling.Activation
{
    public interface ICustomActivator<out T>
    {
        int Order { get; }
        delegate T? ActivatorDelegate(Memory<byte> data, IMarshalingContext ctx);
        T? Activate(object? parent, Memory<byte> data, IMarshalingContext ctx);
    }
    public interface ICustomActivator<out T, TParent> : ICustomActivator<T>
    {
        delegate T? ChildActivatorDelegate(TParent parent, Memory<byte> data, IMarshalingContext ctx);
    }
}
