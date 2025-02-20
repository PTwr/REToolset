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
    public class DefaultCollectionMarshaler
    {
        private readonly ReadHelper readHelper;
        private readonly WriteHelper writeHelper;
        private readonly IOffsetStack offsetStack;
        private readonly IDataBuffer dataBuffer;
        private readonly IHierarchicalFeatureSet features;

        public DefaultCollectionMarshaler(ReadHelper readHelper, WriteHelper writeHelper, IOffsetStack offsetStack, IDataBuffer dataBuffer, IHierarchicalFeatureSet features)
        {
            this.readHelper = readHelper;
            this.writeHelper = writeHelper;
            this.offsetStack = offsetStack;
            this.dataBuffer = dataBuffer;
            this.features = features;
        }

        //TODO custom offset calculators
        //TODO byte alignment
        public void ListWriter<T>(IEnumerable<T?> values, out int bytesWrote)
        {
            bytesWrote = 0;
            foreach (var value in values)
            {
                //TODO some settings for null handling?
                if (value is null) continue;

                writeHelper.Write<T>(value, out var itemBytes);

                bytesWrote += itemBytes;
                offsetStack.AddOffsetShift(itemBytes);
            }
        }

        public (List<(int Offset, T? Value)> data, int bytesRead) ListReader<T>()
        {
            var hasMaxcount = features.HasCollectionCount(out var maxCount);

            int bytesRead = 0;
            List<(int, T?)> temp = new List<(int, T?)>(maxCount);
            while (offsetStack.CurrentAbsoluteOffset < dataBuffer.Length && features.CollectionReadWhile())
            {
                if (hasMaxcount && maxCount == temp.Count) break;

                var value = readHelper.Read<T>(out var itemBytes);

                bytesRead += itemBytes;
                offsetStack.AddOffsetShift(itemBytes);

                temp.Add((itemBytes, value));
            }

            //TODO probably unnecessary and/or breaking on some file formats
            //TODO move into optional premade validator?
            if (hasMaxcount && maxCount > temp.Count)
            {
                throw new InvalidOperationException($"Metadata indicates required length of {maxCount} but only {temp.Count} items have been read. Current absolute offset: {offsetStack.CurrentAbsoluteOffset}. Current data length: {dataBuffer.Length}. {features.GetDebugInfo()}");
            }

            return (temp, bytesRead);
        }
    }
}
