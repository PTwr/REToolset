using BinaryFile.MarshalingDI.ComplexMarshaling;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks
{
    public partial record ObjectCallbacks<TDeclaringType>
    {
        public List<Func<IMarshalerStore, IFieldMarshaler<TDeclaringType>>> FieldMarshalerInitalizers = new List<Func<IMarshalerStore, IFieldMarshaler<TDeclaringType>>>();
        public Func<int> ActivationOrder = () => 0;
        public Func<int> ReadingOrder = () => 0;
        public Func<int> WritingOrder = () => 0;
        public Func<TDeclaringType, int> BytesRead = (x) => 0;
        public Func<TDeclaringType, int> BytesWrote = (x) => 0;
        public Func<object?, TDeclaringType?> DefaultActivator = (x) => default;
    }
}
