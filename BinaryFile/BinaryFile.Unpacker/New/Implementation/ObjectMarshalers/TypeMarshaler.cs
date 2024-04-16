using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker.New.Implementation.ObjectMarshalers.FieldMarshalers;
using BinaryFile.Unpacker.New.Interfaces;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Implementation.ObjectMarshalers
{
    public class TypeMarshaler<TImplementation> : TypeMarshaler<TImplementation, TImplementation>
        where TImplementation : class
    {
        public TypeMarshaler()
        {

        }
        public TypeMarshaler(ITypeMarshaler<TImplementation> parent) : base(parent)
        {
        }
    }
    public class TypeMarshaler<TBase, TImplementation> : ITypeMarshaler<TBase, TImplementation>
        where TBase : class
        where TImplementation : class, TBase
    {
        IMarshalerStore derivedMarshalers = new MarshalerStore();
        ITypeMarshaler<TBase>? Parent;

        List<IOrderedFieldMarshaler<TImplementation>> MarshalingActions = new List<IOrderedFieldMarshaler<TImplementation>>();
        IEnumerable<IOrderedFieldMarshaler<TImplementation>> DeserializingActions => MarshalingActions.Where(i => i.IsDeserializationEnabled);
        IEnumerable<IOrderedFieldMarshaler<TImplementation>> SerializingActions => MarshalingActions.Where(i => i.IsSerializationEnabled);

        public TypeMarshaler(ITypeMarshaler<TBase> parent)
        {
            Parent = parent;
        }

        public TypeMarshaler()
        {
            Parent = null;
        }

        Func<TImplementation, int>? lengthGetter;
        public ITypeMarshaler<TBase, TImplementation> WithByteLengthOf(Func<TImplementation, int> length)
        {
            lengthGetter = length;
            return this;
        }
        public ITypeMarshaler<TBase, TImplementation> WithByteLengthOf(int length) => WithByteLengthOf(i => length);

        List<IOrderedFieldMarshaler<TImplementation>> deserializingActions = new List<IOrderedFieldMarshaler<TImplementation>>();
        public IEnumerable<IOrderedFieldMarshaler<TImplementation>> InheritedDeserializingActions
        {
            get
            {
                if (Parent is not null)
                {
                    var parentStuff =
                        Parent.InheritedDeserializingActions
                        //filter out overriden fields
                        .Where(i => !deserializingActions.Any(j => j.Name == i.Name));
                    return parentStuff.Concat(deserializingActions);
                }
                return deserializingActions.AsReadOnly();
            }
        }

        List<IOrderedFieldMarshaler<TImplementation>> serializingActions = new List<IOrderedFieldMarshaler<TImplementation>>();
        public IEnumerable<IOrderedFieldMarshaler<TImplementation>> InheritedSerializingActions
        {
            get
            {
                if (Parent is not null)
                {
                    var parentStuff =
                        Parent.InheritedSerializingActions
                        //filter out overriden fields
                        .Where(i => !serializingActions.Any(j => j.Name == i.Name));
                    return parentStuff.Concat(serializingActions);
                }
                return serializingActions.AsReadOnly();
            }
        }

        public bool HoldsHierarchyFor<T>()
        {
            return typeof(T).IsAssignableTo(typeof(TImplementation));
        }

        #region Activation
        public IActivator<T>? GetActivatorFor<T>(Span<byte> data, Interfaces.IMarshalingContext ctx)
        {
            var derived = derivedMarshalers.GetActivatorFor<T>(data, ctx);

            if (derived is not null) return derived;

            if (activationCondition?.Invoke(data, ctx) is false) return null;
            return this as IActivator<T>;
        }
        IActivator<TImplementation>.ActivatorConditionDelegate? activationCondition = null;
        public ITypeMarshaler<TBase, TImplementation> WithActivatorCondition(IActivator<TImplementation>.ActivatorConditionDelegate predicate)
        {
            activationCondition = predicate;
            return this;
        }
        IActivator<TImplementation>.CustomActivatorDelegate? customActivator = null;
        public ITypeMarshaler<TBase, TImplementation> WithCustomActivator(IActivator<TImplementation>.CustomActivatorDelegate predicate)
        {
            customActivator = predicate;
            return this;
        }
        public TImplementation Activate(Span<byte> data, Interfaces.IMarshalingContext ctx, object? parent = null)
        {
            return customActivator?.Invoke(data, ctx, parent) ?? ActivationHelper.Activate<TImplementation>(parent);
        }
        #endregion /Activation

        #region Derivation
        public IDeriverableTypeMarshaler<T>? GetMarshalerToDeriveFrom<T>() where T : class
        {
            var derived = derivedMarshalers.GetMarshalerToDeriveFrom<T>();
            if (derived is not null) return derived;

            return this as IDeriverableTypeMarshaler<T>;
        }

        public ITypeMarshaler<TImplementation, TDerived> Derive<TDerived>() where TDerived : class, TImplementation
        {
            var derived = new TypeMarshaler<TImplementation, TDerived>(this);
            derivedMarshalers.RegisterRootMap(derived);

            return derived;
        }
        #endregion /Derivation

        public ITypeMarshaler<TBase, TImplementation> WithMarshalingAction(IOrderedFieldMarshaler<TImplementation> action)
        {
            MarshalingActions.Add(action);
            return this;
        }
        public IOrderedUnaryFieldMarshaler<TImplementation, TFieldType, TFieldType>
            WithField<TFieldType>(Expression<Func<TImplementation, TFieldType>> getter, bool deserialize = true, bool serialize = true)
            => WithField(getter.GetMemberName(), getter, deserialize, serialize);
        public IOrderedUnaryFieldMarshaler<TImplementation, TFieldType, TFieldType>
            WithField<TFieldType>(string name, Expression<Func<TImplementation, TFieldType>> getter, bool deserialize = true, bool serialize = true)
        {
            var action = new OrderedUnaryFieldMarshaler<TImplementation, TFieldType, TFieldType>(name);

            if (deserialize)
            {
                var setter = getter.GenerateToSetter().Compile();
                action
                    .MarshalInto((obj, x, y) => y)
                    .Into(setter);
            }
            if (serialize)
            {
                action
                    .MarshalFrom((obj, x) => x)
                    .From(getter.Compile());
            }

            MarshalingActions.Add(action);

            return action;
        }
        public IOrderedCollectionFieldMarshaler<TImplementation, TFieldType, TFieldType>
            WithCollectionOf<TFieldType>(Expression<Func<TImplementation, IEnumerable<TFieldType>>> getter, bool deserialize = true, bool serialize = true)
            => WithCollectionOf(getter.GetMemberName(), getter, deserialize, serialize);
        public IOrderedCollectionFieldMarshaler<TImplementation, TFieldType, TFieldType>
            WithCollectionOf<TFieldType>(string name, Expression<Func<TImplementation, IEnumerable<TFieldType>>> getter, bool deserialize = true, bool serialize = true)
        {
            var action = new OrderedCollectionFieldMarshaler<TImplementation, TFieldType, TFieldType>(name);

            action
                .MarshalInto((obj, x, y) => y)
                .MarshalFrom((obj, x) => x);

            if (deserialize)
            {
                var setter = getter.GenerateToSetter(tryToUseCtor: true).Compile();
                action
                    .Into(setter);
            }
            if (serialize)
            {
                action
                    .From(getter.Compile());
            }

            MarshalingActions.Add(action);

            return action;
        }

        #region Deserialization
        public IDeserializator<T>? GetDeserializerFor<T>()
        {
            var derived = derivedMarshalers.GetObjectDeserializerFor<T>();
            if (derived is not null) return derived;

            return this as IDeserializator<T, T>;
        }

        public IDeserializator<TImplementation>? GetDeserializerFor()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IOrderedFieldMarshaler<TImplementation>> DerivedDeserializingActions
        {
            get
            {
                if (Parent is not null)
                {
                    var parentStuff =
                        Parent.DerivedDeserializingActions
                        //filter out overriden fields
                        .Where(i => !DeserializingActions.Any(j => j.Name == i.Name));
                    return parentStuff.Concat(DeserializingActions);
                }
                return DeserializingActions;
            }
        }

        public TImplementation DeserializeInto(TImplementation mappedObject, Span<byte> data, Interfaces.IMarshalingContext ctx, out int fieldByteLengh)
        {
            if (mappedObject is null)
                mappedObject = Activate(data, ctx);

            fieldByteLengh = 0;

            //gather and filter actions from base marshalers
            var actions = DerivedDeserializingActions
                .OrderBy(i => i.GetDeserializationOrder(mappedObject));

            //and execute them
            foreach (var action in actions) action.DeserializeInto(mappedObject, data, ctx, out fieldByteLengh);

            fieldByteLengh = lengthGetter?.Invoke(mappedObject) ?? fieldByteLengh;

            return mappedObject;
        }
        #endregion /Deserialization

        #region Serialization
        public ISerializingMarshaler<T>? GetSerializerFor<T>()
        {
            var derived = derivedMarshalers.GetObjectSerializerFor<T>();
            if (derived is not null) return derived;

            return this as ISerializator<T>;
        }

        public IEnumerable<IOrderedFieldMarshaler<TImplementation>> DerivedSerializingActions
        {
            get
            {
                if (Parent is not null)
                {
                    var parentStuff =
                        Parent.DerivedSerializingActions
                        //filter out overriden fields
                        .Where(i => !SerializingActions.Any(j => j.Name == i.Name));
                    return parentStuff.Concat(SerializingActions);
                }
                return SerializingActions;
            }
        }

        public void SerializeFrom(TImplementation mappedObject, ByteBuffer data, Interfaces.IMarshalingContext ctx, out int fieldByteLengh)
        {
            fieldByteLengh = 0;

            //gather and filter actions from base marshalers
            var actions = DerivedSerializingActions
                .OrderBy(i => i.GetSerializationOrder(mappedObject));

            //and execute them
            foreach (var action in actions) action.SerializeFrom(mappedObject, data, ctx, out fieldByteLengh);

            fieldByteLengh = lengthGetter?.Invoke(mappedObject) ?? fieldByteLengh;
        }
        #endregion /Serialization
    }
}
