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
    public static class ReadHelper
    {
        public static T? Read<T>(IMarshalerStore marshalerStore, object? parent, IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out int bytesRead)
        {
            T? value;
            bytesRead = 0;
            if (marshalerStore.TryGetActivatorMarshaler<T>(data, metadata, offsetStack, parent, out var activator))
            {
                value = activator.Activate(data, metadata, offsetStack, parent);

                //if its activated as null, it stays null
                if (value == null) return value;

                if (marshalerStore.TryGetMutableReadMarshaler<T>(value.GetType(), data, metadata, offsetStack, out var mutableReader))
                {
                    try
                    {
                        mutableReader.Read(value, data, out bytesRead, metadata, offsetStack);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"An exception has occured while Reading mutable value into '{value.ToString()}' for field type '{typeof(T).FullName}'. {metadata.GetDebugInfo()}", ex);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Activator found for {typeof(T).FullName} but no corresponding Mutable Read Marshaler found. {metadata.GetDebugInfo()}");
                }
            }
            else if (marshalerStore.TryGetReadMarshaler<T>(data, metadata, offsetStack, out var reader))
            {
                try
                {
                    value = reader.Read(data, out bytesRead, metadata, offsetStack);
                }
                catch (Exception ex)
                {
                    throw new Exception($"An exception has occured while Reading immutable value for field type '{typeof(T).FullName}'. {metadata.GetDebugInfo()}", ex);
                }
            }
            else throw new InvalidOperationException($"No read marshaler found for {typeof(T).FullName}. {metadata.GetDebugInfo()}");
            return value;
        }
    }
}
