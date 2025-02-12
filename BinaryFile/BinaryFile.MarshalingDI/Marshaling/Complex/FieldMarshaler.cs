using Autofac;
using Autofac.Features.Metadata;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    public class ObjectMarshaler<TDeclaringType>
    {
        public ObjectMarshaler(IMarshalerStore marshalerStore)
        {
            this.marshalerStore = marshalerStore;
        }

        List<Func<IMarshalerStore, IFieldMarshaler<TDeclaringType>>> fieldMarshalerInitalizers = new List<Func<IMarshalerStore, IFieldMarshaler<TDeclaringType>>>();
        private readonly IMarshalerStore marshalerStore;

        public void Read(TDeclaringType declaringObject, IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            foreach(var fieldMarshalerInitalizer in fieldMarshalerInitalizers)
            {
                fieldMarshalerInitalizer(marshalerStore).ReadField(declaringObject, data, out _, metadata, offsetStack);
            }
        }

        public class FieldMarshaler<TMarshaledType> : IFieldMarshaler<TDeclaringType>
        {
            private readonly IMarshalerStore marshalerStore;
            private readonly Callbacks callbacks;

            private FieldMarshaler(IMarshalerStore marshalerStore, Callbacks callbacks)
            {
                this.marshalerStore = marshalerStore;
                this.callbacks = callbacks;
            }

            private class Callbacks
            {
                public Func<TDeclaringType, TMarshaledType?>? getter = null;
                public Action<TDeclaringType, TMarshaledType?>? setter = null;
                public Func<TDeclaringType, (int offset, OffsetRelation relation)>? offsetCalculator = null;
                public Func<TDeclaringType, int>? readOrderCalculator = null;
                public Func<TDeclaringType, int>? writeOrderCalculator = null;
            }

            public class Builder
            {
                private Callbacks callbacks = new Callbacks();

                public void Register(ObjectMarshaler<TDeclaringType> objectMarshaler)
                {
                    objectMarshaler.fieldMarshalerInitalizers.Add((store) => new FieldMarshaler<TMarshaledType>(store, callbacks));
                }

                public Builder From(Func<TDeclaringType, TMarshaledType?> getter)
                {
                    callbacks.getter = getter;
                    return this;
                }
                public Builder Into(Action<TDeclaringType, TMarshaledType?> setter)
                {
                    callbacks.setter = setter;
                    return this;
                }
                public Builder AtOffset(Func<TDeclaringType, (int offset, OffsetRelation relation)> offsetCalculator)
                {
                    callbacks.offsetCalculator = offsetCalculator;
                    return this;
                }
                public Builder WithReadOrderOf(Func<TDeclaringType, int> readOrderCalculator)
                {
                    callbacks.readOrderCalculator = readOrderCalculator;
                    return this;
                }
                public Builder WithWriteOrderOf(Func<TDeclaringType, int> writeOrderCalculator)
                {
                    callbacks.writeOrderCalculator = writeOrderCalculator;
                    return this;
                }
            }

            public void ReadField(TDeclaringType declaringObject, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            {
                if (callbacks.setter is null)
                    throw new Exception($"Read Marshaling executed without setter method. {metadata.GetDebugInfo()}");

                if (callbacks.offsetCalculator is null)
                    throw new Exception($"Read Marshaling executed without offset calculator method. {metadata.GetDebugInfo()}");

                var offset = callbacks.offsetCalculator(declaringObject);
                offsetStack.Push(offset.offset, offset.relation);

                var value = ReadHelper.Read<TMarshaledType>(marshalerStore, declaringObject, data, metadata, offsetStack, out bytesRead);

                offsetStack.Pop();

                callbacks.setter(declaringObject, value);
            }

            public void WriteField(TDeclaringType declaringObject, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            {
                if (callbacks.getter is null)
                    throw new Exception($"Write Marshaling executed without getter method. {metadata.GetDebugInfo()}");

                if (callbacks.offsetCalculator is null)
                    throw new Exception($"Write Marshaling executed without offset calculator method. {metadata.GetDebugInfo()}");

                var offset = callbacks.offsetCalculator(declaringObject);
                offsetStack.Push(offset.offset, offset.relation);

                var value = callbacks.getter(declaringObject);

                offsetStack.Pop();

                bytesRead = 0;
                if (value is null) return;

                WriteHelper.Write<TMarshaledType>(marshalerStore, value, data, metadata, offsetStack, out bytesRead);
            }
        }
    }
}
