using Autofac;
using Autofac.Features.AttributeFilters;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers
{
    public partial class ObjectMarshaler<TDeclaringType> : IFullMutableMarshaler<TDeclaringType>
    {
        private readonly MarshalingFeaturesBuilder marshalingFeatures;
        private readonly IEnumerable<IFieldMarshaler<TDeclaringType>> fieldMarshalers;
        private readonly ILifetimeScope container;
        private readonly IMarshalerStore marshalerStore;
        private readonly ObjectCallbacks<TDeclaringType> callbacks;
        private readonly IFeatureSetStack features;

        public ObjectMarshaler(
            ILifetimeScope container, 
            IMarshalerStore marshalerStore, 
            IFeatureSetStack features, 
            ObjectCallbacks<TDeclaringType> callbacks,
            IEnumerable<IFieldMarshaler<TDeclaringType>> fieldMarshalers,
            MarshalingFeaturesBuilder marshalingFeatures)
        {
            this.container = container;
            this.marshalerStore = marshalerStore;
            this.callbacks = callbacks;
            this.features = features;
            this.fieldMarshalers = fieldMarshalers;
            this.marshalingFeatures = marshalingFeatures;
        }

        public TDeclaringType? Activate()
        {
            return callbacks.DefaultActivator(container);
        }

        public void Read(TDeclaringType value, out int bytesRead)
        {
            features.Push(features.GetDebugInfo());
            marshalingFeatures.ApplyReadFeatures(features, container);
            features.SetCurrentObject(value);

            //TODO cache?
            foreach (var fieldMarshaler in fieldMarshalers
                .Where(x => x.IsForReading())
                .OrderBy(x => x.ReadOrder())
                )
            {
                fieldMarshaler.ReadField();
            }

            bytesRead = callbacks.BytesRead(value);

            features.Pop();
        }

        public void Write(TDeclaringType value, out int bytesWrote)
        {
            features.Push(features.GetDebugInfo());
            marshalingFeatures.ApplyWriteFeatures(features, container);
            features.SetCurrentObject(value);

            //TODO cache?
            foreach (var fieldMarshaler in fieldMarshalers
                .Where(x => x.IsForWriting())
                .OrderBy(x => x.WriteOrder())
                )
            {
                fieldMarshaler.WriteField();
            }
            bytesWrote = callbacks.BytesWrote(value);

            features.Pop();
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
