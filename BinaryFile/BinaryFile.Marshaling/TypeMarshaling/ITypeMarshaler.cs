using BinaryFile.Marshaling.Activation;
using BinaryFile.Marshaling.FieldMarshaling;
using BinaryFile.Marshaling.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryDataHelper;
using System.Linq.Expressions;

namespace BinaryFile.Marshaling.TypeMarshaling
{
    public interface ITypelessMarshaler
    {
        //EWWW!
        object? ActivateTypeless(object? parent, Memory<byte> data, IMarshalingContext ctx, Type? type = null);
        object? DeserializeTypeless(object? obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength);
        void SerializeTypeless(object? obj, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength);
    }
    public interface ITypeMarshaler
    {
        bool IsFor(Type t);
    }
    public interface ITypeMarshaler<TRoot> : ITypeMarshaler
    {
        TRoot? Deserialize(TRoot? obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength);
        void Serialize(TRoot? obj, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLength);
    }
    public interface ITypeMarshalerWithActivation<TRoot> : ITypeMarshaler<TRoot>
    {
        TRoot? Activate(object? parent, Memory<byte> data, IMarshalingContext ctx, Type? type = null);
    }
    public interface ITypeMarshaler<TRoot, TImplementation> : ITypeMarshalerWithActivation<TRoot>
    {
        IEnumerable<IOrderedFieldMarshaler<TImplementation>> DerivedMarshalingActions { get; }
        void HandleBeforeDeserializationEvent(TImplementation obj, Memory<byte> data, IMarshalingContext ctx);
    }
    public interface ITypeMarshaler<TRoot, in TBase, TImplementation> : ITypeMarshaler<TRoot, TImplementation>
        where TBase : class, TRoot
        where TImplementation : class, TBase, TRoot
    {
        ITypeMarshaler<TRoot, TImplementation, TDerived> Derive<TDerived>()
            where TDerived : class, TRoot, TBase, TImplementation;

        ITypeMarshaler<TRoot, TBase, TImplementation> WithCustomActivator(ICustomActivator<TImplementation> customActivator);
        ITypeMarshaler<TRoot, TBase, TImplementation> WithMarshalingAction(IOrderedFieldMarshaler<TImplementation> action);

        IOrderedUnaryFieldMarshaler<TImplementation, TFieldType, TFieldType>
            WithField<TFieldType>(Expression<Func<TImplementation, TFieldType>> getter, bool deserialize = true, bool serialize = true);
        IOrderedUnaryFieldMarshaler<TImplementation, TFieldType, TFieldType>
            WithField<TFieldType>(string name, Expression<Func<TImplementation, TFieldType>> getter, bool deserialize = true, bool serialize = true);

        IOrderedCollectionFieldMarshaler<TImplementation, TFieldType, TFieldType>
            WithCollectionOf<TFieldType>(Expression<Func<TImplementation, IEnumerable<TFieldType>>> getter, bool deserialize = true, bool serialize = true);
        IOrderedCollectionFieldMarshaler<TImplementation, TFieldType, TFieldType>
            WithCollectionOf<TFieldType>(string name, Expression<Func<TImplementation, IEnumerable<TFieldType>>> getter, bool deserialize = true, bool serialize = true);

        IOrderedCollectionFieldMarshaler<TImplementation, TFieldType, TMarshalingType>
           WithCollectionOf<TFieldType, TMarshalingType>(Expression<Func<TImplementation, IEnumerable<TFieldType>>> getter, bool deserialize = true, bool serialize = true);
        IOrderedCollectionFieldMarshaler<TImplementation, TFieldType, TMarshalingType>
            WithCollectionOf<TFieldType, TMarshalingType>(string name, Expression<Func<TImplementation, IEnumerable<TFieldType>>> getter, bool deserialize = true, bool serialize = true);

        ITypeMarshaler<TRoot, TBase, TImplementation> WithByteLengthOf(Func<TImplementation, int> getter);
        ITypeMarshaler<TRoot, TBase, TImplementation> WithByteLengthOf(int length);
        ITypeMarshaler<TRoot, TBase, TImplementation> BeforeDeserialization(Action<TImplementation, Memory<byte>, IMarshalingContext> eventHandler);
    }
}
