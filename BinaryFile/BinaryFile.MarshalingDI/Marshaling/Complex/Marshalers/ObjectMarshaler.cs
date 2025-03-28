using Autofac;
using Autofac.Features.AttributeFilters;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers
{
    public partial class ObjectMarshaler<TDeclaringType> : IFullMarshaler<TDeclaringType>
    {
        private readonly MarshalingFeaturesBuilder marshalingFeatures;
        private readonly IEnumerable<IFieldMarshaler<TDeclaringType>> fieldMarshalers;
        private readonly ILifetimeScope container;
        private readonly ObjectCallbacks<TDeclaringType> callbacks;
        private readonly IFeatureSetStack features;

        public ObjectMarshaler(
            ILifetimeScope container, 
            IFeatureSetStack features, 
            ObjectCallbacks<TDeclaringType> callbacks,
            IEnumerable<IFieldMarshaler<TDeclaringType>> fieldMarshalers,
            MarshalingFeaturesBuilder marshalingFeatures)
        {
            this.container = container;
            this.callbacks = callbacks;
            this.features = features;
            this.fieldMarshalers = fieldMarshalers;
            this.marshalingFeatures = marshalingFeatures;
        }

        public TDeclaringType? Activate()
        {
            var value = callbacks.DefaultActivator(container);

            //ensure value is stored even if ReadHelper is activating for lower interface
            features.SetCurrentObject(value);
            
            return value;
        }

        public TDeclaringType Read(out int bytesRead)
        {
            TDeclaringType value = features.GetCurentObject<TDeclaringType>();
            features.Push();
            marshalingFeatures.ApplyReadFeatures(features, container);

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

            return value;
        }

        public void Write(TDeclaringType value, out int bytesWrote)
        {
            features.Push();
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

        public bool IsForActivating()
        {
            return callbacks.IsForActivating(container);
        }
        public bool IsForMutableReading()
        {
            return callbacks.IsForReading(container);
        }
    }
}
