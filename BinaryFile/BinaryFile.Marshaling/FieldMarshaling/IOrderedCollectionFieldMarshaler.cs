using BinaryFile.Marshaling.MarshalingContext;

namespace BinaryFile.Marshaling.FieldMarshaling
{
    public interface IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType>
        : IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType>>
        where TDeclaringType : class
    {
    }
    public interface IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
        : IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
        where TInterface : class, IOrderedCollectionFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
        where TDeclaringType : class
    {
        public delegate bool BreakWhenDelegate(TDeclaringType declaringObject, IEnumerable<TFieldType> items, Memory<byte> data, IMarshalingContext context);
        TInterface BreakWhen(BreakWhenDelegate handler);
        TInterface From(Func<TDeclaringType, IEnumerable<TFieldType>> getter);
        TInterface Into(Action<TDeclaringType, IEnumerable<TFieldType>> setter);
        TInterface WithCountOf(Func<TDeclaringType, int> func);
        TInterface WithCountOf(int count);
        TInterface WithItemByteAlignment(Func<TDeclaringType, int> func);
        TInterface WithItemByteAlignment(int padding);
        TInterface WithItemByteLengthOf(Func<TDeclaringType, int> func);
        TInterface WithItemByteLengthOf(int itemLength);
        TInterface WithItemNullPadToAlignment(Func<TDeclaringType, int> func);
        TInterface WithItemNullPadToAlignment(int padding);

        public delegate void AfterSerializingItemEvent(TDeclaringType mappedObject, TFieldType item, int fieldByteLength, int index, int offset);
        TInterface AfterSerializingItem(AfterSerializingItemEvent hook);
    }
}
