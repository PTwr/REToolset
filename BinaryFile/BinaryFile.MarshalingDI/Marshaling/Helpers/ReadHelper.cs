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
    public class ReadHelper
    {
        private readonly IMarshalerStore marshalerStore;
        private readonly IFeatureSetStack features;

        public ReadHelper(IMarshalerStore marshalerStore, IFeatureSetStack features)
        {
            this.marshalerStore = marshalerStore;
            this.features = features;
        }

        public T? Read<T>(out int bytesRead)
        {
            T? value;
            bytesRead = 0;
            if (marshalerStore.TryGetActivatorMarshaler<T>(out var activator))
            {
                value = activator.Activate();

                //if its activated as null, it stays null
                if (value == null) return value;

                if (marshalerStore.TryGetMutableReadMarshaler<T>(value.GetType(), out var mutableReader))
                {
                    try
                    {
                        mutableReader.Read(value, out bytesRead);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"An exception has occured while Reading mutable value into '{value.ToString()}' for field type '{typeof(T).FullName}'. {features.GetDebugInfo()}", ex);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Activator found for {typeof(T).FullName} but no corresponding Mutable Read Marshaler found. {features.GetDebugInfo()}");
                }
            }
            else if (marshalerStore.TryGetReadMarshaler<T>(out var reader))
            {
                try
                {
                    value = reader.Read(out bytesRead);
                }
                catch (Exception ex)
                {
                    throw new Exception($"An exception has occured while Reading immutable value for field type '{typeof(T).FullName}'. {features.GetDebugInfo()}", ex);
                }
            }
            else throw new InvalidOperationException($"No read marshaler found for {typeof(T).FullName}. {features.GetDebugInfo()}");
            return value;
        }
    }
}
