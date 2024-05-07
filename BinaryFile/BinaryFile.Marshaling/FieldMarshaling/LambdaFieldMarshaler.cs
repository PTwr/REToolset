using BinaryDataHelper;
using BinaryFile.Marshaling.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.FieldMarshaling
{
    public class LambdaFieldMarshaler<TMappedType, TFieldType>
            : OrderedFieldMarshaler<TMappedType, TFieldType, TFieldType, LambdaFieldMarshaler<TMappedType, TFieldType>>
            where TMappedType : class
    {
        private readonly Action<TMappedType>? deserialize;
        private readonly Action<TMappedType>? serialize;

        public LambdaFieldMarshaler(string name, Action<TMappedType>? deserialize = null, Action<TMappedType>? serialize = null) : base(name)
        {
            this.deserialize = deserialize;
            this.serialize = serialize;

            IsDeserializationEnabled = deserialize is not null;
            IsSerializationEnabled = serialize is not null;
        }

        public override void DeserializeInto(TMappedType mappedObject, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            deserialize(mappedObject);
            fieldByteLength = 0;
        }

        public override void SerializeFrom(TMappedType mappedObject, IByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            serialize(mappedObject);
            fieldByteLength = 0;
        }
    }
}
