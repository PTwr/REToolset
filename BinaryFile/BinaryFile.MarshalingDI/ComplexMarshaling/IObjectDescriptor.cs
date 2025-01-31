using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using System;
using BinaryDataHelper;
using ReflectionHelper;
using Microsoft.VisualBasic.FileIO;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using BinaryFile.MarshalingDI.TypeMarshaling;
using Autofac.Core;
using Autofac;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BinaryFile.MarshalingDI.ObjectMarshaling
{
    public interface IMarshalerStore
    {
        IReadMarshaler<TFieldType> GetReadMarshaler<TFieldType, TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            where TMarshaledType : TFieldType;
        IMutableReadMarshaler<TFieldType> GetMutableReadMarshalerz<TFieldType, TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            where TFieldType : class
            where TMarshaledType : class, TFieldType;
        IWriteMarshaler<TMarshaledType> GetWriteMarshaler<TMarshaledType>(TMarshaledType value);
        IActivatorMarshaler<TMarshaledType> GetActivatorMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent);
    }
    public class DefaultMarshalerStore : IMarshalerStore
    {
        private readonly IContainer container;

        public DefaultMarshalerStore(IContainer container)
        {
            this.container = container;
        }

        private IEnumerable<TOut> DIEnumerate<TOut>(Func<TOut, bool> condition)
            where TOut : IOrderedMarshaler
        {
            var marshalers = container.Resolve<IEnumerable<TOut>>()
                .OrderBy(x => x.Order);
            foreach (var marshalerCandidate in marshalers)
            {
                if (marshalerCandidate is TOut fieldMarshaler
                    &&
                    condition(fieldMarshaler))
                {
                    yield return fieldMarshaler;
                }
            }
        }
        private IEnumerable<TOut> DIEnumerate<TOut>(Type TExact, Func<TOut, bool> condition)
            where TOut : IOrderedMarshaler
        {
            var marshalerType = typeof(TOut).GetGenericTypeDefinition().MakeGenericType(TExact);
            var marshalerCollectionType = typeof(IEnumerable<>).MakeGenericType(marshalerType);

            var marshalers = ((IEnumerable<IOrderedMarshaler>)container.Resolve(marshalerCollectionType))
                .Reverse()
                .OrderByDescending(x => x.Order);

            foreach (var marshalerCandidate in marshalers)
            {
                if (marshalerCandidate is TOut fieldMarshaler
                    &&
                    condition(fieldMarshaler))
                {
                    yield return fieldMarshaler;
                }
            }
        }

        public IActivatorMarshaler<TMarshaledType> GetActivatorMarshaler<TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            foreach (var type in typeof(TMarshaledType).EnumerateTypeHierarchy()
                .Concat(typeof(TMarshaledType).GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach (var marshalerCandidate in DIEnumerate<IActivatorMarshaler<TMarshaledType>>(type, (x) => (x.IsForActivating(data, metadata, offsetStack, parent))))
                {
                    //return first matching marshaler
                    return marshalerCandidate;
                }
            }
            if (container.TryResolve<IActivatorMarshaler<TMarshaledType>>(out var exactMarshaler))
            {
                return exactMarshaler;
            }

            throw new TypeLoadException($"Failed to locate ActivatorMarshaler for {typeof(TMarshaledType).FullName}");
        }

        public IReadMarshaler<TFieldType> GetReadMarshaler<TFieldType, TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            where TMarshaledType : TFieldType
        {
            foreach (var type in typeof(TMarshaledType).EnumerateTypeHierarchy()
                .Concat(typeof(TMarshaledType).GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach(var marshalerCandidate in DIEnumerate<IReadMarshaler<TFieldType>>(type, (x) => x.IsForReading(data, metadata, offsetStack)))
                {
                    //return first matching marshaler
                    return marshalerCandidate;
                }
            }

            throw new TypeLoadException($"Failed to locate ReadMarshaler for {typeof(TMarshaledType).FullName}");
        }

        public IMutableReadMarshaler<TFieldType> GetMutableReadMarshalerz<TFieldType, TMarshaledType>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
            where TFieldType : class
            where TMarshaledType : class, TFieldType
        {
            foreach (var type in typeof(TMarshaledType).EnumerateTypeHierarchy()
                .Concat(typeof(TMarshaledType).GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach (var marshalerCandidate in DIEnumerate<IMutableReadMarshaler<TFieldType>>(type, (x) => x.IsForMutableReading(data, metadata, offsetStack)))
                {
                    //return first matching marshaler
                    return marshalerCandidate;
                }
            }
            throw new TypeLoadException($"Failed to locate ReadMarshaler for {typeof(TMarshaledType).FullName}");
        }

        public IWriteMarshaler<TMarshaledType> GetWriteMarshaler<TMarshaledType>(TMarshaledType value)
        {
            foreach (var type in typeof(TMarshaledType).EnumerateTypeHierarchy()
                .Concat(typeof(TMarshaledType).GetInterfaces()))
            {
                //starting from exact type and crawling down, then through interfaces
                foreach (var marshalerCandidate in DIEnumerate<IWriteMarshaler<TMarshaledType>>(type, (x) => x.IsForWriting(value)))
                {
                    //return first matching marshaler
                    return marshalerCandidate;
                }
            }

            throw new TypeLoadException($"Failed to locate WriteMarshaler for {typeof(TMarshaledType).FullName}");
        }
    }

    //invoked by IObjectDescriptor, thus TParent is always known
    //difference from Primitive Marshaler is Parent and void
    //fielddescriptor should first TryResolve(IPrimitiveMarshaler), then look for IObjectDescriptor?
    public interface IFieldDescriptor<TParent>
    {
        IMarshalingMetadata Metadata { get; }

        void Read(IMarshalerStore marshalerStore, TParent parent, IDataBuffer data, out int bytesRead, IOffsetStack offsetStack);
        void Write(IMarshalerStore marshalerStore, TParent parent, IDataBuffer data, out int bytesWrote, IOffsetStack offsetStack);

        public delegate (int offset, bool success) CustomOffsetCalculator(TParent parent, IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack);
    }

    public class DefaultFieldDescriptor<TParent, TValue> : IFieldDescriptor<TParent, TValue>
    {
        public DefaultFieldDescriptor(Action<TParent, TValue> setter, Func<TParent, TValue> getter, IMarshalingMetadata metadata)
        {
            Setter = setter;
            Getter = getter;
            Metadata = metadata;
        }

        public virtual IMarshalingMetadata Metadata { get; private set; }

        protected Action<TParent, TValue> Setter { get; private set; }
        protected Func<TParent, TValue> Getter { get; private set; }

        public virtual void Read(IMarshalerStore marshalerStore, TParent parent, IDataBuffer data, out int bytesRead, IOffsetStack offsetStack)
        {
            var value = marshalerStore
                .GetReadMarshaler<TValue, TValue>(data, Metadata, offsetStack)
                .Read(data, out bytesRead, Metadata, offsetStack);

            Setter(parent, value);
        }

        public virtual void Write(IMarshalerStore marshalerStore, TParent parent, IDataBuffer data, out int bytesWrote, IOffsetStack offsetStack)
        {
            var value = Getter(parent);

            marshalerStore
                .GetWriteMarshaler<TValue>(value)
                .Write(value, data, out bytesWrote, Metadata, offsetStack);
        }
    }

    //TODO implement IMarshalingMetadata and just pass Descriptor?
    public interface IFieldDescriptor<TParent, TValue>
    {
        public delegate (TValue value, bool success) CustomGetter(TParent parent);
        public delegate bool CustomSetter(TParent parent, TValue value);
    }

    //TODO shoudl descriptors be marshalers??
    //FLOW:
    //1. root object type, or field type, is known, ObjectDescriptor<TRoot/TField> is injected
    //2a. descriptor.Activate returns object instance, and optionally another descriptor (for exact implementation)
    //2b. object is provided, exact type has to be tested to find closest match :/ this sucks
    //3. implementationDescriptor.Read/Write iterates over FieldDescriptors and magic happens
    //4a. Field is Primitive, step out
    //4b. Field is Complex, recurse back to #1

    //resolving exact ObjectDescriptor for existing object during serialization
    //can be moved into descriptor by finding descriptor by field type, Root deserialization would require providing marshaling type/interface
    //assuming all field types have matching (top-level) marshaler defined
    //but it would still be shit to find it automagically

    //checking bo exact type will prevent usage of convienient inheritance when building/editing graph
    //reimplementing IsFor<T> on marshalers is not feasible with DI as Order cant be easily controlled, and wildcard resolving would suuuuuck
    //type crawling starting from exact, then its base class, then interfaces, then base of base, until object is reached?
    //but interfaces can inherit interfaces! should concrete type always be preffered?

    //will be invoked on root level, where root type is known
    //or through FieldDescriptors, where Field.TValue becomse Object.TValue
    public interface IObjectDescriptor<TType>
        where TType : class
    {
        //returning implementation descriptor will allow it to be configurable (Eg, implementation switch depending on header bytes)
        //doing that by testing actual Type is what messed up previous codebase
        //TODO generic typed parent??
        public TType Activate(object? parent, IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack);
        public void Read(TType value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack);
        public void Write(TType value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack);

        public delegate (TType? obj, bool success) CustomActivator(object? parent, IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack);
    }

    public class ObjectDescriptorBuilder<TType>
        where TType : class
    {
        List<IObjectDescriptor<TType>.CustomActivator> activators = new();

        public ObjectDescriptorBuilder<TType> WithPatternActivator<TImplementationType>(
            byte?[] pattern, int offset = 0, OffsetRelation offsetRelation = OffsetRelation.Segment, string tag = null)
            where TImplementationType : TType
        {
            return this.WithCustomActivator<TImplementationType>(
                (object? parent, IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack) =>
                {
                    var slice = data.AsSpan(offsetStack.CalculateAbsoluteOffset(offset, offsetRelation, this.tag ?? tag));
                    if (slice.StartsWith(pattern))
                    {
                        var obj = ActivationHelper.Activate<TImplementationType>(parent);
                        return (obj, true);
                    }
                    return (default, false);
                }
                );
        }
        public ObjectDescriptorBuilder<TType> WithCustomActivator<TImplementationType>(
            IObjectDescriptor<TType>.CustomActivator customActivator)
            where TImplementationType : TType
        {
            activators.Add(customActivator);
            return this;
        }

        string? tag = null;
        public ObjectDescriptorBuilder<TType> WithTag(string tag)
        {
            this.tag = tag;
            return this;
        }

        public FieldDescriptorBuilder<TType, TValue> WithField<TValue>()
        {
            return new FieldDescriptorBuilder<TType, TValue>();
        }

        IObjectDescriptor<TType> Build()
        {
            return new ObjectDescriptor<TType>(activators, tag);
        }
    }

    public class FieldDescriptorBuilder<TParent, TValue>
    {
        bool read = true;
        bool write = true;
        public FieldDescriptorBuilder()
        {

        }

        public FieldDescriptorBuilder<TParent, TValue> FromLamda(Expression<Func<TParent, TValue>> member)
        {
            return this;
        }
        public FieldDescriptorBuilder<TParent, TValue> ForMember(MemberInfo prop)
        {
            return this;
        }
        public FieldDescriptorBuilder<TParent, TValue> ForProperty(PropertyInfo prop)
        {
            return this;
        }
        public FieldDescriptorBuilder<TParent, TValue> ForField(FieldInfo field)
        {
            return this;
        }
        public FieldDescriptorBuilder<TParent, TValue> WithCustomGetter()
        {
            return this;
        }
        public FieldDescriptorBuilder<TParent, TValue> WithCustomSetter()
        {
            return this;
        }

        string? tag = null;
        public FieldDescriptorBuilder<TParent, TValue> WithTag(string tag)
        {
            this.tag = tag;
            return this;
        }

        public FieldDescriptorBuilder<TParent, TValue> OnlyRead()
        {
            read = true;
            write = false;
            return this;
        }
        public FieldDescriptorBuilder<TParent, TValue> OnlyWrite()
        {
            read = false;
            write = true;
            return this;
        }
        public FieldDescriptorBuilder<TParent, TValue> ReadAndWrite()
        {
            read = true;
            write = true;
            return this;
        }

        int offset = 0;
        public FieldDescriptorBuilder<TParent, TValue> AtOffset(int offset)
        {
            this.offset = offset;
            return this;
        }
        OffsetRelation offserRelation = OffsetRelation.Segment;
        public FieldDescriptorBuilder<TParent, TValue> RelativeTo(OffsetRelation offsetRelation)
        {
            this.offserRelation = offsetRelation;
            return this;
        }

        List<IFieldDescriptor<TParent>.CustomOffsetCalculator> customOffsetCalculators = new();
        public FieldDescriptorBuilder<TParent, TValue> WithCustomOffsetCalculator(IFieldDescriptor<TParent>.CustomOffsetCalculator customOffsetCalculator)
        {
            customOffsetCalculators.Add(customOffsetCalculator);
            return this;
        }

        public FieldDescriptorBuilder<TParent, TValue> WithExpectedValueOf(TValue expectedValue)
        {
            throw new NotImplementedException();
            return this;
        }
    }

    public class ObjectDescriptor<TType>
        : IObjectDescriptor<TType>
        where TType : class
    {
        private readonly List<IObjectDescriptor<TType>.CustomActivator> activators;
        private readonly string tag;

        public ObjectDescriptor(List<IObjectDescriptor<TType>.CustomActivator> activators, string tag)
        {
            this.activators = activators;
            this.tag = tag;
        }

        public TType Activate(object? parent, IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            if (typeof(TType).IsInterface) throw new NotSupportedException($"Can not instantiate interface '{typeof(TType).FullName}'. Tag: {tag}");

            var obj = ActivationHelper.Activate<TType>(parent);
            return obj;
        }

        public void Read(TType value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            throw new NotImplementedException();
        }

        public void Write(TType value, IDataBuffer data, out int bytesRead, IMarshalingMetadata metadata, IOffsetStack offsetStack)
        {
            throw new NotImplementedException();
        }
    }
}
