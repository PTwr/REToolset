using BinaryDataHelper;
using BinaryFile.Unpacker.Metadata;
using BinaryFile.Unpacker.New.Implementation.ObjectMarshalers.FieldMarshalers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Unpacker.New.Interfaces
{
    public interface IOrderedFieldMarshaler<in TDeclaringType>
    {
        string Name { get; }
        int GetOrder(TDeclaringType mappedObject);
        int GetSerializationOrder(TDeclaringType mappedObject);
        int GetDeserializationOrder(TDeclaringType mappedObject);

        bool IsDeserializationEnabled { get; }
        bool IsSerializationEnabled { get; }

        void DeserializeInto(TDeclaringType mappedObject, Span<byte> data, IMarshalingContext ctx, out int fieldByteLengh);
        void SerializeFrom(TDeclaringType mappedObject, ByteBuffer data, IMarshalingContext ctx, out int fieldByteLengh);
    }
    public interface IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
        : IOrderedFieldMarshaler<TDeclaringType>
        where TDeclaringType : class
        where TInterface : class, IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
    {
        TInterface AtOffset(Func<TDeclaringType, int> offsetGetter);
        TInterface AtOffset(int offset);

        public delegate TMarshaledType MarshalingValueGetter(TDeclaringType declaringObject, TFieldType item);
        public delegate TFieldType MarshalingValueSetter(TDeclaringType declaringObject, TFieldType item, TMarshaledType marshaledValue);
        TInterface MarshalFrom(MarshalingValueGetter getter);
        TInterface MarshalInto(MarshalingValueSetter setter);

        TInterface RelativeTo(Func<TDeclaringType, OffsetRelation> offsetRelationGetter);
        TInterface RelativeTo(OffsetRelation offsetRelation);

        TInterface WithByteLengthOf(Func<TDeclaringType, int> lengthGetter);
        TInterface WithByteLengthOf(int length);

        TInterface WithDeserializationOrderOf(Func<TDeclaringType, int> orderGetter);
        TInterface WithDeserializationOrderOf(int order);

        TInterface WithOrderOf(Func<TDeclaringType, int> orderGetter);
        TInterface WithOrderOf(int order);

        TInterface WithSerializationOrderOf(Func<TDeclaringType, int> orderGetter);
        TInterface WithSerializationOrderOf(int order);
    }
}
