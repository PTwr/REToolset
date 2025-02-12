using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling.Helpers;
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
        public void ListWriter<T>(IEnumerable<T?> values, IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, out int bytesWrote)
        {
            bytesWrote = 0;
            foreach (var value in values)
            {
                //TODO some settings for null handling?
                if (value is null) continue;

                WriteHelper.Write<T>(marshalerStore, value, data, metadata, offsetStack, out var itemBytes);

                bytesWrote += itemBytes;
                offsetStack.AddOffsetShift(itemBytes);
            }
        }

        //TODO add ReadWhile(lambda) option
        //TODO FieldDescriptor has to handle casting (offset,item) pair to exact collection, can be taken care with .WriteInto clause with some default handling for common collections
        public (List<(int Offset, T? Value)> data, int bytesRead) ListReader<T>(IDataBuffer data, IMarshalingMetadata metadata, IOffsetStack offsetStack, object? parent)
        {
            var hasMaxcount = metadata.HasCollectionCount(out var maxCount);
            var readWhile = metadata.GetCollectionReadWhile<T>();

            int bytesRead = 0;
            List<(int, T?)> temp = new List<(int, T?)>(maxCount);
            while (offsetStack.CurrentAbsoluteOffset < data.Length && readWhile.ReadWhile(temp, data, metadata, offsetStack))
            {
                if (hasMaxcount && maxCount == temp.Count) break;

                var value = ReadHelper.Read<T>(marshalerStore, parent, data, metadata, offsetStack, out var itemBytes);

                bytesRead += itemBytes;
                offsetStack.AddOffsetShift(itemBytes);

                temp.Add((itemBytes, value));
            }

            //TODO probably unnecessary and/or breaking on some file formats
            //TODO move into optional premade validator?
            if (hasMaxcount && maxCount > temp.Count)
            {
                throw new InvalidOperationException($"Metadata indicates required length of {maxCount} but only {temp.Count} items have been read. Current absolute offset: {offsetStack.CurrentAbsoluteOffset}. Current data length: {data.Length}. {metadata.GetDebugInfo()}");
            }

            return (temp, bytesRead);
        }
    }
}
