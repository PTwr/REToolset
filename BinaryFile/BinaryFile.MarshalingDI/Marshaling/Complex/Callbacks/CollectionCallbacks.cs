namespace BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks
{
    public class CollectionCallbacks<TDeclaringType, TMarshaledType>
        : BaseFieldCallbacks<TDeclaringType>
    {
        public Func<TDeclaringType, int>? ItemCount;
        public Func<TDeclaringType, IEnumerable<TMarshaledType>>? Getter;
        public Action<TDeclaringType, List<(int offsetInCollection, TMarshaledType? item)>>? Setter;
    }
}
