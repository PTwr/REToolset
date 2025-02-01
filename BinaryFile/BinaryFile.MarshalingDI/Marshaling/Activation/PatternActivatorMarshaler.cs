using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryDataHelper;

namespace BinaryFile.MarshalingDI.Marshaling.Activating
{
    public class PatternActivatorMarshaler<TMarshaledType> : IActivatorMarshaler<TMarshaledType>
        where TMarshaledType : new()
    {
        private readonly byte?[] pattern;

        public PatternActivatorMarshaler(IEnumerable<byte?> pattern, int order = 0)
        {
            this.pattern = pattern.ToArray();
            Order = order;
        }

        public int Order { get; }

        public bool IsForActivating(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            return data.AsSpan(offsetStack.CurrentAbsoluteOffset).StartsWith(pattern);
        }

        public TMarshaledType Activate(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            return new();
        }
    }
}
