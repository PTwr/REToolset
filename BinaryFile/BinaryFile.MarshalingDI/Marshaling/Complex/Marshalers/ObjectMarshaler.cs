using Autofac;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers
{
    public partial class ObjectMarshaler<TDeclaringType> : IFullMutableMarshaler<TDeclaringType>
    {
        private readonly IMarshalerStore marshalerStore;
        private readonly ObjectCallbacks<TDeclaringType> callbacks;

        public ObjectMarshaler(IMarshalerStore marshalerStore, ObjectCallbacks<TDeclaringType> callbacks)
        {
            this.marshalerStore = marshalerStore;
            this.callbacks = callbacks;
        }

        public TDeclaringType? Activate(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            return callbacks.DefaultActivator(parent);
        }

        public void Read(TDeclaringType value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            //TODO cache?
            foreach (var fieldMarshaler in callbacks.FieldMarshalerInitalizers
                .Select(x => x(marshalerStore))
                .Where(x => x.IsForReading(value))
                .OrderBy(x => x.ReadOrder(value))
                )
            {
                fieldMarshaler.ReadField(value, data, out _, metadata, offsetStack);
            }

            bytesRead = callbacks.BytesRead(value);
        }

        public void Write(TDeclaringType value, IDataBuffer data, out int bytesWrote, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            //TODO cache?
            foreach (var fieldMarshaler in callbacks.FieldMarshalerInitalizers
                .Select(x => x(marshalerStore))
                .Where(x => x.IsForWriting(value))
                .OrderBy(x => x.WriteOrder(value))
                )
            {
                //TODO return objectByteSize (required for collection item offsets
                fieldMarshaler.WriteField(value, data, out _, metadata, offsetStack);
            }
            bytesWrote = callbacks.BytesWrote(value);
        }

        public int Order(EMarshalingType marshalingType)
        {
            switch (marshalingType)
            {
                case EMarshalingType.Activation:
                    return callbacks.ActivationOrder();
                case EMarshalingType.Reading:
                    return callbacks.ReadingOrder();
                case EMarshalingType.Writing:
                    return callbacks.WritingOrder();
                default:
                    throw new ArgumentException($"Unkown {nameof(EMarshalingType)} value of {marshalingType}!");
            }
        }

    }
}
