using Autofac;
using Autofac.Features.Metadata;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using BinaryFile.MarshalingDI.Marshaling.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers
{
    public abstract partial class FieldMarshaler<TDeclaringType, TMarshaledType, TCallbacks> : IFieldMarshaler<TDeclaringType> where TCallbacks : BaseFieldCallbacks<TDeclaringType>
    {
        protected MarshalingFeatures MarshalingFeatures = new MarshalingFeatures();
        protected readonly IHierarchicalFeatureSet features;
        protected readonly TCallbacks callbacks;
        protected readonly ILifetimeScope container;
        protected readonly IOffsetStack offsetStack;
        protected readonly ReadHelper readHelper;
        protected readonly WriteHelper writeHelper;

        public FieldMarshaler(IHierarchicalFeatureSet features, IOffsetStack offsetStack, ReadHelper readHelper, WriteHelper writeHelper, TCallbacks callbacks, ILifetimeScope container)
        {
            this.features = features;
            this.callbacks = callbacks;
            this.container = container;
            this.offsetStack = offsetStack;
            this.readHelper = readHelper;
            this.writeHelper = writeHelper;
        }

        public bool IsForReading()
            => callbacks.MarshalingType(container).HasFlag(EMarshalingType.Reading);
        public bool IsForWriting()
            => callbacks.MarshalingType(container).HasFlag(EMarshalingType.Reading);

        public int ReadOrder()
            => callbacks.ReadOrderCalculator(container);
        public int WriteOrder()
            => callbacks.WriteOrderCalculator(container);

        public abstract void ReadField();
        public abstract void WriteField();
    }
}
