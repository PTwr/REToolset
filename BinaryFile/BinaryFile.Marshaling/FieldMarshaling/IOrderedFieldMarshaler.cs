using BinaryDataHelper;
using BinaryFile.Marshaling.Common;
using BinaryFile.Marshaling.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.FieldMarshaling
{
    public interface IOrderedFieldMarshaler<in TDeclaringType>
    {
        string Name { get; }
        int GetOrder(TDeclaringType mappedObject);
        int GetSerializationOrder(TDeclaringType mappedObject);
        int GetDeserializationOrder(TDeclaringType mappedObject);

        bool IsDeserializationEnabled { get; }
        bool IsSerializationEnabled { get; }

        void DeserializeInto(TDeclaringType mappedObject, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength);
        void SerializeFrom(TDeclaringType mappedObject, IByteBuffer data, IMarshalingContext ctx, out int fieldByteLength);
    }
    public interface IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
        : IOrderedFieldMarshaler<TDeclaringType>
        where TDeclaringType : class
        where TInterface : class, IOrderedFieldMarshaler<TDeclaringType, TFieldType, TMarshaledType, TInterface>
    {
        public delegate void AfterSerializingEvent(TDeclaringType mappedObject, int fieldByteLength);
        TInterface AfterSerializing(AfterSerializingEvent hook);

        TInterface AtOffset(Func<TDeclaringType, int> offsetGetter);
        TInterface AtOffset(int offset);

        public delegate TMarshaledType? MarshalingValueGetter(TDeclaringType declaringObject, TFieldType item);
        public delegate TFieldType? MarshalingValueSetter(TDeclaringType declaringObject, TFieldType? item, TMarshaledType? marshaledValue);
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

        TInterface WithEncoding(Func<TDeclaringType, Encoding> encodingGetter);
        TInterface WithEncoding(Encoding encoding);

        TInterface WithNullTerminator(Func<TDeclaringType, bool> nullTermiantionGetter);
        TInterface WithNullTerminator(bool isNullTerminated = true);

        delegate TFieldType CustomActivatorEvent(TDeclaringType parent, Memory<byte> data, IMarshalingContext ctx);
        [Obsolete("This is not used currently, delete or implement properly")]
        TInterface WithCustomActivator(CustomActivatorEvent activator);

        TInterface WithValidator(Func<TDeclaringType, TFieldType, bool> validateFunc);

        TInterface AsNestedFile(Func<TDeclaringType, bool> getter);
        TInterface AsNestedFile(bool isNested = true);
    }
}
