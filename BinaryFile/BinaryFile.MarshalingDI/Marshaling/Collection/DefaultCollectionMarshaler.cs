using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.ObjectMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace BinaryFile.MarshalingDI.Marshaling.Collection
{
    public class DefaultCollectionMarshaler
    {
        private readonly IMarshalerStore marshalerStore;

        public DefaultCollectionMarshaler(IMarshalerStore marshalerStore)
        {
            this.marshalerStore = marshalerStore;
        }
        public (List<KeyValuePair<int, T>>, int) ListReadear<T>(IDataBuffer data, IMarshalingMetadata meta, IOffsetStack stack)
        {
            var bytesRead = 0;
            List<KeyValuePair<int, T>> temp = new List<KeyValuePair<int, T>>((int)(meta.ItemCount ?? 0));
            while (stack.CurrentAbsoluteOffset < data.Length)
            {
                //TODO try Activate and MutableRead before ImmutableRead 
                var m = marshalerStore!.GetReadMarshaler<T, T>(data, meta, stack);
                var a = m.Read(data, out var br, meta, stack);

                bytesRead += br;
                stack.AddOffsetShift(br);

                temp.Add(new KeyValuePair<int, T>(br, a));

                if (meta.ItemCount.HasValue && meta.ItemCount.Value == temp.Count) break;
            }
            return (temp, bytesRead);
        }
    }
}
