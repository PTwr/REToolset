using Autofac;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks
{
    public class ObjectCallbacks<TDeclaringType>
        : BaseCallbacks<TDeclaringType>
    {
        public Func<int> ActivationOrder = () => 0;
        public Func<int> ReadingOrder = () => 0;
        public Func<int> WritingOrder = () => 0;
        public Func<TDeclaringType, int> BytesRead = (x) => 0;
        public Func<TDeclaringType, int> BytesWrote = (x) => 0;
        public Func<ILifetimeScope, TDeclaringType?> DefaultActivator = (x) => default;
        public List<Func<ILifetimeScope, (bool activated, TDeclaringType? value)>> Activators = [];
        public Func<ILifetimeScope, bool> IsForActivating = (x) => true;
        public Func<ILifetimeScope, bool> IsForReading = (x) => true;
    }
}
