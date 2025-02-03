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
    //TODO passing all the options through params would SUUUUCK
    //TODO turn it into FieldDescriptor?
    //TODO change into CollectionMarshaler and UnaryMarshalers?
    public class DefaultCollectionMarshaler
    {
        private readonly IMarshalerStore marshalerStore;

        public DefaultCollectionMarshaler(IMarshalerStore marshalerStore)
        {
            this.marshalerStore = marshalerStore;
        }

        //TODO custom offset calculators
        //TODO byte alignment
        public int ListWriter<T>(IEnumerable<T> values, IDataBuffer data, IMarshalingMetadata meta, IOffsetStack stack)
        {
            int bytesRead = 0;
            foreach (var value in values)
            {
                int itemBytes = 0; 
                
                if (marshalerStore.TryGetWriteMarshaler<T>(value, out var reader))
                {
                    reader.Write(value, data, out itemBytes, meta, stack);
                }
                else throw new InvalidOperationException($"No read marshaler found for {typeof(T).FullName}.");

                bytesRead += itemBytes;
                stack.AddOffsetShift(itemBytes);
            }
            return bytesRead;
        }

        //TODO add ReadWhile(lambda) option
        //TODO FieldDescriptor has to handle casting (offset,item) pair to exact collection, can be taken care with .WriteInto clause with some default handling for common collections
        public (List<(int Offset, T Value)> data, int bytesRead) ListReader<T>(IDataBuffer data, IMarshalingMetadata meta, IOffsetStack stack, object? parent)
        {
            int bytesRead = 0;
            List<(int, T)> temp = new List<(int, T)>((int)(meta.ItemCount ?? 0));
            while (stack.CurrentAbsoluteOffset < data.Length)
            {
                if (meta.ItemCount.HasValue && meta.ItemCount.Value == temp.Count) break;

                int itemBytes = 0;
                T value = default!;
                if (marshalerStore.TryGetActivatorMarshaler<T>(data, meta, stack, parent, out var activator))
                {
                    value = activator.Activate(data, meta, stack, parent);

                    //TODO what if there is activator but no mutablereadeR? O_O
                    if (marshalerStore.TryGetMutableReadMarshaler<T>(value.GetType(), data, meta, stack, out var mutableReader))
                    {
                        mutableReader.Read(value, data, out itemBytes, meta, stack);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Activator found for {typeof(T).FullName} but no corresponding Mutable Read Marshaler found.");
                    }
                }
                else if (marshalerStore.TryGetReadMarshaler<T>(data, meta, stack, out var reader))
                {
                    value = reader.Read(data, out itemBytes, meta, stack);
                }
                else throw new InvalidOperationException($"No read marshaler found for {typeof(T).FullName}.");

                bytesRead += itemBytes;
                stack.AddOffsetShift(itemBytes);

                temp.Add((itemBytes, value));
            }

            if (meta.ItemCount.HasValue && meta.ItemCount.Value > temp.Count)
            {
                throw new InvalidOperationException($"Metadata indicates required length of {meta.ItemCount} but only {temp.Count} items has been read. Current absolute offset: {stack.CurrentAbsoluteOffset}. Current data length: {data.Length}");
            }

            return (temp, bytesRead);
        }
    }
}
