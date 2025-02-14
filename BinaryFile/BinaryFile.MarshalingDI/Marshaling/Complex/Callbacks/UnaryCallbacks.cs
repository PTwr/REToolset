namespace BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks
{
    public class UnaryCallbacks<TDeclaringType, TMarshaledType>
        : BaseFieldCallbacks<TDeclaringType>
    {
        public Func<TDeclaringType, TMarshaledType?>? Getter = null;
        public Action<TDeclaringType, TMarshaledType?>? Setter = null;
    }
}
