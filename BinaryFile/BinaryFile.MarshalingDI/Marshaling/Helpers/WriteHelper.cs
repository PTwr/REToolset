using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Marshaling.Helpers
{
    public static class WriteHelper
    {
        public static void Write<T>(IMarshalerStore marshalerStore, T value, IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out int bytesRead)
        {
            if (marshalerStore.TryGetWriteMarshaler<T>(value, out var writer))
            {
                writer.Write(value, data, out bytesRead, metadata, offsetStack);
            }
            else throw new InvalidOperationException($"No Write marshaler found for {typeof(T).FullName}. {metadata.GetDebugInfo()}");
        }
    }
}
