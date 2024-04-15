using BinaryDataHelper;
using Microsoft.VisualBasic.FileIO;

namespace BinaryFile.Unpacker.New.Interfaces
{
    public interface IOrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType>
        : IOrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, IOrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType>>
        where TDeclaringType : class
    {

    }
    public interface IOrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
        : IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
        where TInterface : class, IOrderedUnaryFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
        where TDeclaringType : class
    {
        TInterface From(Func<TDeclaringType, TFieldType> getter);
        TInterface Into(Action<TDeclaringType, TFieldType> setter);
    }
}