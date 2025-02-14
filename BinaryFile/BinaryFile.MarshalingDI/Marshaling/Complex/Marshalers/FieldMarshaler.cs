using Autofac.Features.Metadata;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
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

namespace BinaryFile.MarshalingDI.Marshaling.Complex.Marshalers
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

        protected IMarshalingMetadata GetFieldReadMetadata(TDeclaringType declaringObject, IMarshalingMetadata upstreamMetadata)
            => upstreamMetadata.Concat(callbacks.ReadMetadataSource.Select(x => x(declaringObject)));
        protected IMarshalingMetadata GetFieldWriteMetadata(TDeclaringType obj, IMarshalingMetadata upstreamMetadata)
            => upstreamMetadata.Concat(callbacks.WriteMetadataSource.Select(x => x(obj)));

        public bool IsForReading(TDeclaringType declaringObject) => callbacks.MarshalingType(declaringObject).HasFlag(EMarshalingType.Reading);
        public bool IsForWriting(TDeclaringType declaringObject) => callbacks.MarshalingType(declaringObject).HasFlag(EMarshalingType.Reading);

        public int ReadOrder(TDeclaringType declaringObject) => callbacks.ReadOrderCalculator(declaringObject);
        public int WriteOrder(TDeclaringType declaringObject) => callbacks.WriteOrderCalculator(declaringObject);
    }
}
