namespace BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks
{
    public class UnaryCallbacks<TDeclaringType, TMarshaledType>
        : BaseCallbacks<TDeclaringType>
    {
        public Func<TDeclaringType, TMarshaledType?>? Getter = null;
        public Action<TDeclaringType, TMarshaledType?>? Setter = null;

        public Func<TDeclaringType, bool> AfterReadValidator = (x) => true;
        public Func<TDeclaringType, bool> BeforeWriteValidator = (x) => true;
    }
}
