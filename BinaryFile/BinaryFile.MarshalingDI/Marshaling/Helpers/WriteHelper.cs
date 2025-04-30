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
    public class WriteHelper
    {
        private readonly IMarshalerStore marshalerStore;
        private readonly IFeatureSetStack features;

        public WriteHelper(IMarshalerStore marshalerStore, IFeatureSetStack features)
        {
            this.marshalerStore = marshalerStore;
            this.features = features;
        }

        public void Write<T>(T value, out int bytesWrote)
        {
            if (marshalerStore.TryGetWriteMarshaler<T>(value, out var writer))
            {
                try
                {
                    writer.Write(value,  out bytesWrote);
                }
                catch (Exception ex)
                {
                    throw new Exception($"An exception has occured while Writing value '{value?.ToString()}'. {features.GetDebugInfo()}", ex);
                }
            }
            else throw new InvalidOperationException($"No Write marshaler found for {typeof(T).FullName}. {features.GetDebugInfo()}");

            var padding = features.Find<IPadding>(null, 0);
            padding?.GetValue()?.Pad(bytesWrote, out bytesWrote);
        }
    }
}
