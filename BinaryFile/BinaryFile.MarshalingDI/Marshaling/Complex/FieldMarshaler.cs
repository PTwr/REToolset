using Autofac.Features.Metadata;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Complex.Callbacks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    public abstract partial class FieldMarshaler<TDeclaringType, TMarshaledType, TCallbacks>
        where TCallbacks : BaseFieldCallbacks<TDeclaringType>
    {
        protected readonly IMarshalerStore marshalerStore;
        protected readonly TCallbacks callbacks;

        public FieldMarshaler(IMarshalerStore marshalerStore, TCallbacks callbacks)
        {
            this.marshalerStore = marshalerStore;
            this.callbacks = callbacks;
        }

        public abstract partial record Callbacks
        {
            public Func<TDeclaringType, (int offset, OffsetRelation relation)>? OffsetCalculator = null;
            public Func<TDeclaringType, int> ReadOrderCalculator = (x) => 0;
            public Func<TDeclaringType, int> WriteOrderCalculator = (x) => 0;

            public Func<TDeclaringType, EMarshalingType> MarshalingType = (x) => EMarshalingType.Reading | EMarshalingType.Writing;
        }


    }
}
