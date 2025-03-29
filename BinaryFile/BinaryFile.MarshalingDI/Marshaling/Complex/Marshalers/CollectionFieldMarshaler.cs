using Autofac;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Collection;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Helpers;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers
{
    public class CollectionFieldMarshaler<TDeclaringType, TMarshaledType> :
        FieldMarshaler<TDeclaringType, TMarshaledType, CollectionCallbacks<TDeclaringType, TMarshaledType>>,
        IFieldMarshaler<TDeclaringType>
    {
        private readonly DefaultCollectionMarshaler collectionMarshaler;

        public CollectionFieldMarshaler(DefaultCollectionMarshaler defaultCollectionMarshaler, IFeatureSetStack features, IOffsetStack offsetStack, ReadHelper readHelper, WriteHelper writeHelper, CollectionCallbacks<TDeclaringType, TMarshaledType> callbacks, ILifetimeScope container, MarshalingFeaturesBuilder marshalingFeatures) : base(features, offsetStack, readHelper, writeHelper, callbacks, container, marshalingFeatures)
        {
            this.collectionMarshaler = defaultCollectionMarshaler;
        }

        public override void ReadField()
        {
            features.Push();
            marshalingFeatures.ApplyReadFeatures(features, container);
            //shift curent obj to parent obj
            var declaringObject = features.GetCurentObject<TDeclaringType>();
            features.SetCurrentObjectAsParent<TDeclaringType>();

            if (callbacks.Setter is null)
                throw new Exception($"Collection Read Marshaling executed without setter method. {features.GetDebugInfo()}");

            if (callbacks.OffsetCalculator is null)
                throw new Exception($"Collection Read Marshaling executed without offset calculator method. {features.GetDebugInfo()}");

            var offset = callbacks.OffsetCalculator(container);
            offsetStack.Push(offset.offset, offset.relation);

            var result = collectionMarshaler.ListReader<TMarshaledType>();

            callbacks.Setter(declaringObject, result.data, result.bytesRead);

            callbacks.AfterReadValidator(container);

            offsetStack.Pop();
            features.Pop();
        }

        public override void WriteField()
        {
            features.Push();
            marshalingFeatures.ApplyWriteFeatures(features, container);
            //shift curent obj to parent obj
            var declaringObject = features.GetCurentObject<TDeclaringType>();
            features.SetCurrentObjectAsParent<TDeclaringType>();

            //TODO throw if false
            callbacks.BeforeWriteValidator(container);

            if (callbacks.Getter is null)
                throw new Exception($"Collection Write Marshaling executed without getter method. {features.GetDebugInfo()}");

            if (callbacks.OffsetCalculator is null)
                throw new Exception($"Collection Write Marshaling executed without offset calculator method. {features.GetDebugInfo()}");

            var values = callbacks.Getter(declaringObject);
            if (values is null || !values.Any()) return;

            var offset = callbacks.OffsetCalculator(container);
            offsetStack.Push(offset.offset, offset.relation);

            collectionMarshaler.ListWriter(values, out var bytesWrote);
            callbacks.OnAfterWrite(container, bytesWrote);

            offsetStack.Pop();
            features.Pop();
        }
    }
}
