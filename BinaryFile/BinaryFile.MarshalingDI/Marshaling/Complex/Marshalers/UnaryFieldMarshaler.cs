using Autofac;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Helpers;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers
{
    public partial class UnaryFieldMarshaler<TDeclaringType, TMarshaledType> :
        FieldMarshaler<TDeclaringType, TMarshaledType, UnaryCallbacks<TDeclaringType, TMarshaledType>>,
        IFieldMarshaler<TDeclaringType>
    {
        public UnaryFieldMarshaler(IHierarchicalFeatureSet features, IOffsetStack offsetStack, ReadHelper readHelper, WriteHelper writeHelper, UnaryCallbacks<TDeclaringType, TMarshaledType> callbacks, ILifetimeScope container, MarshalingFeatures marshalingFeatures) : base(features, offsetStack, readHelper, writeHelper, callbacks, container, marshalingFeatures)
        {
        }

        //TODO make configurable as well and move default logic to helper? Maybe nested class as well, to be abple to receive Callbacks
        //TODO clean up processor of all non-callback scum!!!
        public override void ReadField()
        {
            features.Push(features.GetDebugInfo());
            features.AddFeatureRange(marshalingFeatures.ReadFeatures);
            //shift curent obj to parent obj
            var declaringObject = features.GetCurentObject<TDeclaringType>();
            features.AddValueFeature(declaringObject, 1, EMetadataNames.ParentObject.ToString());

            if (callbacks.Setter is null)
                throw new Exception($"Read Marshaling executed without setter method. {features.GetDebugInfo()}");

            if (callbacks.OffsetCalculator is null)
                throw new Exception($"Read Marshaling executed without offset calculator method. {features.GetDebugInfo()}");

            var offset = callbacks.OffsetCalculator(container);
            offsetStack.Push(offset.offset, offset.relation);

            var value = readHelper.Read<TMarshaledType>(out var bytesRead);

            callbacks.Setter(declaringObject, value);

            callbacks.AfterReadValidator(container);

            offsetStack.Pop();
            features.Pop();
        }

        public override void WriteField()
        {
            features.Push(features.GetDebugInfo());
            features.AddFeatureRange(marshalingFeatures.WriteFeatures);
            //shift curent obj to parent obj
            var declaringObject = features.GetCurentObject<TDeclaringType>();
            features.AddValueFeature(declaringObject, 1, EMetadataNames.ParentObject.ToString());

            callbacks.BeforeWriteValidator(container);

            if (callbacks.Getter is null)
                throw new Exception($"Write Marshaling executed without getter method. {features.GetDebugInfo()}");

            if (callbacks.OffsetCalculator is null)
                throw new Exception($"Write Marshaling executed without offset calculator method. {features.GetDebugInfo()}");

            var value = callbacks.Getter(declaringObject);
            if (value is null) return;

            var offset = callbacks.OffsetCalculator(container);
            offsetStack.Push(offset.offset, offset.relation);

            writeHelper.Write(value, out var bytesWrote);
            callbacks.OnAfterWrite(container, bytesWrote);

            offsetStack.Pop();
            features.Pop();
        }
    }
}
