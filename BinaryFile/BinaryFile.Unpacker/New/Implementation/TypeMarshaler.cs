using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker.New.Interfaces;
using ReflectionHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BinaryFile.Unpacker.New.Implementation
{
    public class TypeMarshaler<TBase, TImplementation> : ITypeMarshaler<TBase, TImplementation>
        where TBase : class
        where TImplementation : class, TBase
    {
        IMarshalerStore derrivedMarshalers = new MarshalerStore();
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
            var derrived = derrivedMarshalers.GetActivatorFor<T>(data, ctx);

            if (derrived is not null) return derrived;

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

        #region Derrivation
        public IDerriverableTypeMarshaler<T>? GetMarshalerToDerriveFrom<T>() where T : class
        {
            var derrived = derrivedMarshalers.GetMarshalerToDerriveFrom<T>();
            if (derrived is not null) return derrived;

            return this as IDerriverableTypeMarshaler<T>;
        }

        public ITypeMarshaler<TImplementation, TDerrived> Derrive<TDerrived>() where TDerrived : class, TImplementation
        {
            var derrived = new TypeMarshaler<TImplementation, TDerrived>(this);
            derrivedMarshalers.RegisterRootMap(derrived);

            return derrived;
        }
        #endregion /Derrivation

        #region Deserialization
        public IDeserializator<T>? GetDeserializerFor<T>()
        {
            var derrived = derrivedMarshalers.GetObjectDeserializerFor<T>();
            if (derrived is not null) return derrived;

            return this as IDeserializator<T, T>;
        }

        public IDeserializator<TImplementation>? GetDeserializerFor()
        {
            throw new NotImplementedException();
        }
        
        public ITypeMarshaler<TBase, TImplementation> WithDeserializingAction(IOrderedFieldMarshaler<TImplementation> action)
        {
            MarshalingActions.Add(action);
            return this;
        }

        public IEnumerable<IOrderedFieldMarshaler<TImplementation>> DerrivedDeserializingActions
        {
            get
            {
                if (Parent is not null)
                {
                    var parentStuff =
                        Parent.DerrivedDeserializingActions
                        //filter out overriden fields
                        .Where(i => !DeserializingActions.Any(j => j.Name == i.Name));
                    return parentStuff.Concat(DeserializingActions);
                }
                return DeserializingActions;
            }
        }

        public TImplementation DeserializeInto(TImplementation mappedObject, Span<byte> data, Interfaces.IMarshalingContext ctx, out int fieldByteLengh)
        {
            fieldByteLengh = 0;

            //gather and filter actions from base marshalers
            var actions = DerrivedDeserializingActions
                .OrderBy(i => i.GetDeserializationOrder(mappedObject));

            //and execute them
            foreach (var action in actions) action.DeserializeInto(mappedObject, data, ctx, out fieldByteLengh);

            //TODO execute custom Length hook

            return mappedObject;
        }
        #endregion /Deserialization

        #region Serialization
        public ISerializingMarshaler<T>? GetSerializerFor<T>()
        {
            var derrived = derrivedMarshalers.GetObjectSerializerFor<T>();
            if (derrived is not null) return derrived;

            return this as ISerializator<T>;
        }

        public ITypeMarshaler<TBase, TImplementation> WithSerializingAction(IOrderedFieldMarshaler<TImplementation> action)
        {
            MarshalingActions.Add(action);
            return this;
        }

        public IEnumerable<IOrderedFieldMarshaler<TImplementation>> DerrivedSerializingActions
        {
            get
            {
                if (Parent is not null)
                {
                    var parentStuff =
                        Parent.DerrivedSerializingActions
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
            var actions = DerrivedSerializingActions
                .OrderBy(i => i.GetSerializationOrder(mappedObject));

            //and execute them
            foreach (var action in actions) action.SerializeFrom(mappedObject, data, ctx, out fieldByteLengh);

            //TODO execute custom Length hook
        }
        #endregion /Serialization
    }
}
