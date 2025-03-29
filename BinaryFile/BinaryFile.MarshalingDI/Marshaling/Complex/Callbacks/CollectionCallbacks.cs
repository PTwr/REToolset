namespace BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks
{
    public class CollectionCallbacks<TDeclaringType, TMarshaledType>
        : BaseFieldCallbacks<TDeclaringType>
    {
        public Func<TDeclaringType, IEnumerable<TMarshaledType>>? Getter;
        public Action<TDeclaringType, List<(int Offset, TMarshaledType? Value)>, int>? Setter;
    }
}
