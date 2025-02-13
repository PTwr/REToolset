using Autofac;
using Autofac.Features.Metadata;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Helpers;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    public class ObjectMarshaler<TDeclaringType> : IFullMutableMarshaler<TDeclaringType>
    {
        private readonly IMarshalerStore marshalerStore;
        private readonly Callbacks callbacks;

        private ObjectMarshaler(IMarshalerStore marshalerStore, Callbacks callbacks)
        {
            this.marshalerStore = marshalerStore;
            this.callbacks = callbacks;
        }

        private record Callbacks
        {
            public List<Func<IMarshalerStore, IFieldMarshaler<TDeclaringType>>> FieldMarshalerInitalizers = new List<Func<IMarshalerStore, IFieldMarshaler<TDeclaringType>>>();
            public Func<int> ActivationOrder = () => 0;
            public Func<int> ReadingOrder = () => 0;
            public Func<int> WritingOrder = () => 0;
            public Func<TDeclaringType, int> BytesRead => (x) => 0;
            public Func<TDeclaringType, int> BytesWrote => (x) => 0;
            public Func<object?, TDeclaringType?> DefaultActivator = (x) => default;
        }

        //TODO private (file?) and return as interface?
        public class Builder
        {
            private Callbacks callbacks = new Callbacks();

            public void RegisterInDI(ContainerBuilder containerBuilder)
            {
                containerBuilder.Register((ctx) =>
                    {
                        var store = ctx.Resolve<IMarshalerStore>();
                        var objMarshaler = new ObjectMarshaler<TDeclaringType>(store, callbacks);

                        return objMarshaler;
                    })
                    .As<IActivatorMarshaler<TDeclaringType>>()
                    .As<IMutableReadMarshaler<TDeclaringType>>()
                    .As<IWriteMarshaler<TDeclaringType>>();
            }

            /// <summary>
            /// Activator executed if all conditional activators pass through without activation
            /// </summary>
            /// <param name="activator"></param>
            /// <returns></returns>
            public Builder WithDefaultActivator(Func<object?, TDeclaringType?> activator)
            {
                callbacks.DefaultActivator = activator;
                return this;
            }

            public Builder RegisterFieldMarshalerInitializer(Func<IMarshalerStore, IFieldMarshaler<TDeclaringType>> initializer)
            {
                callbacks.FieldMarshalerInitalizers.Add(initializer);
                return this;
            }

            public FieldMarshaler<TMarshaledType>.Builder WithField<TMarshaledType>()
            {
                var builder = new FieldMarshaler<TMarshaledType>.Builder(this);
                return builder;
            }
            public void WithCollection<TMarshaledType>()
            {
                //TODO fallback to normal marshaling if marshaler for specific collection type is registered? Huge optimization for stuff like byte[]
                //TODO unifying unary field and collections would suck and pollute fluent with unnecessary config methods, keep separate?
                throw new NotImplementedException();
            }
        }

        public TDeclaringType? Activate(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            return callbacks.DefaultActivator(parent);
        }

        public void Read(TDeclaringType value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            //TODO filter for ForReading and order by ReadingOrder
            //TODO cache? or init on ctor? field marshalers should be reentrant by default
            foreach (var fieldMarshalerInitalizer in callbacks.FieldMarshalerInitalizers)
            {
                //TODO return objectByteSize (required for collection item offsets
                fieldMarshalerInitalizer(marshalerStore).ReadField(value, data, out _, metadata, offsetStack);
            }

            bytesRead = callbacks.BytesRead(value);
        }

        public void Write(TDeclaringType value, IDataBuffer data, out int bytesWrote, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            //TODO filter for ForWriting and order by WritingOrder
            //TODO cache? or init on ctor? field marshalers should be reentrant by default
            foreach (var fieldMarshalerInitalizer in callbacks.FieldMarshalerInitalizers)
            {
                //TODO return objectByteSize (required for collection item offsets
                fieldMarshalerInitalizer(marshalerStore).WriteField(value, data, out _, metadata, offsetStack);
            }
            bytesWrote = callbacks.BytesWrote(value);
        }

        public int Order(MarshalingType marshalingType)
        {
            switch (marshalingType)
            {
                case MarshalingType.Activation:
                    return callbacks.ActivationOrder();
                case MarshalingType.Reading:
                    return callbacks.ReadingOrder();
                case MarshalingType.Writing:
                    return callbacks.WritingOrder();
                default:
                    throw new ArgumentException($"Unkown {nameof(MarshalingType)} value of {marshalingType}!");
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

            private record Callbacks
            {
                public Func<TDeclaringType, TMarshaledType?>? Getter = null;
                public Action<TDeclaringType, TMarshaledType?>? Setter = null;
                public Func<TDeclaringType, (int offset, OffsetRelation relation)>? OffsetCalculator = null;
                public Func<TDeclaringType, int>? ReadOrderCalculator = null;
                public Func<TDeclaringType, int>? WriteOrderCalculator = null;
            }

            public class Builder
            {
                private readonly ObjectMarshaler<TDeclaringType>.Builder parent;
                private Callbacks callbacks = new Callbacks();

                protected internal Builder(ObjectMarshaler<TDeclaringType>.Builder parent)
                {
                    this.parent = parent;
                }

                public ObjectMarshaler<TDeclaringType>.Builder Done()
                {
                    parent.RegisterFieldMarshalerInitializer((store) => new FieldMarshaler<TMarshaledType>(store, callbacks));
                    return parent;
                }

                public Builder WriteFrom(Func<TDeclaringType, TMarshaledType?> getter)
                {
                    callbacks.Getter = getter;
                    return this;
                }
                public Builder ReadInto(Action<TDeclaringType, TMarshaledType?> setter)
                {
                    callbacks.Setter = setter;
                    return this;
                }
                public Builder AtOffset(Func<TDeclaringType, (int offset, OffsetRelation relation)> offsetCalculator)
                {
                    callbacks.OffsetCalculator = offsetCalculator;
                    return this;
                }
                public Builder WithReadOrderOf(Func<TDeclaringType, int> readOrderCalculator)
                {
                    callbacks.ReadOrderCalculator = readOrderCalculator;
                    return this;
                }
                public Builder WithWriteOrderOf(Func<TDeclaringType, int> writeOrderCalculator)
                {
                    callbacks.WriteOrderCalculator = writeOrderCalculator;
                    return this;
                }
            }

            //TODO make configurable as well and move default logic to helper? Maybe nested class as well, to be abple to receive Callbacks
            //TODO clean up processor of all non-callback scum!!!
            public void ReadField(TDeclaringType declaringObject, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            {
                if (callbacks.Setter is null)
                    throw new Exception($"Read Marshaling executed without setter method. {metadata.GetDebugInfo()}");

                if (callbacks.OffsetCalculator is null)
                    throw new Exception($"Read Marshaling executed without offset calculator method. {metadata.GetDebugInfo()}");

                var offset = callbacks.OffsetCalculator(declaringObject);
                offsetStack.Push(offset.offset, offset.relation);

                var value = ReadHelper.Read<TMarshaledType>(marshalerStore, declaringObject, data, metadata, offsetStack, out bytesRead);

                offsetStack.Pop();

                callbacks.Setter(declaringObject, value);
            }

            public void WriteField(TDeclaringType declaringObject, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            {
                if (callbacks.Getter is null)
                    throw new Exception($"Write Marshaling executed without getter method. {metadata.GetDebugInfo()}");

                if (callbacks.OffsetCalculator is null)
                    throw new Exception($"Write Marshaling executed without offset calculator method. {metadata.GetDebugInfo()}");

                bytesRead = 0;
                var value = callbacks.Getter(declaringObject);
                if (value is null) return;

                var offset = callbacks.OffsetCalculator(declaringObject);
                offsetStack.Push(offset.offset, offset.relation);

                WriteHelper.Write<TMarshaledType>(marshalerStore, value, data, metadata, offsetStack, out bytesRead);

                offsetStack.Pop();
            }
        }
    }
}
