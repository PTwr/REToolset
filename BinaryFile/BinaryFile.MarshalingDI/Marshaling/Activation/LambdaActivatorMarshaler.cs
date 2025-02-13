using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Marshaling.Activating
{
    public class LambdaActivatorMarshaler<TMarshaledType> : IActivatorMarshaler<TMarshaledType>
    {
        private readonly Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, object?, TMarshaledType> activator;
        private readonly Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, object?, bool> condition;

        public LambdaActivatorMarshaler(
            Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, object?, TMarshaledType> activator,
            Func<IDataBuffer, IMarshalingMetadata, IOffsetStack, object?, bool>? condition = null)
        {
            this.activator = activator;
            this.condition = condition ?? ((d, m, o, p) => true);
        }

        public bool IsForActivating(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            return condition(data, metadata, offsetStack, parent);
        }

        public TMarshaledType Activate(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            return activator(data, metadata, offsetStack, parent);
        }
    }
}
