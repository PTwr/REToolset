using BinaryDataHelper;
using BinaryFile.Marshaling.Context;

namespace BinaryFile.Marshaling.TypeMarshaling
{
    public class MarshalerWrapper<T> : ITypeMarshaler<T>
    {
        public int Order => typeMarshaler.Order;

        private readonly ITypelessMarshaler typeMarshaler;

        public T? Activate(object? parent, Memory<byte> data, IMarshalingContext ctx, Type? type = null)
        {
            var x = typeMarshaler.ActivateTypeless(parent, data, ctx, type);
            if (x is T) return (T)x;
            return default;
        }

        public T? Deserialize(T? obj, object? parent, Memory<byte> data, IMarshalingContext ctx, out int fieldByteLength)
        {
            var x = typeMarshaler.DeserializeTypeless(obj, parent, data, ctx, out fieldByteLength);
            if (x is T) return (T)x;
            return default;
        }

        public bool IsFor(Type t)
        {
            throw new NotImplementedException();
        }

        public void Serialize(T? obj, IByteBuffer data, IMarshalingContext ctx, out int fieldByteLength)
        {
            typeMarshaler.SerializeTypeless(obj, data, ctx, out fieldByteLength);
        }

        public MarshalerWrapper(ITypelessMarshaler typeMarshaler)
        {
            this.typeMarshaler = typeMarshaler;
        }
    }
}
