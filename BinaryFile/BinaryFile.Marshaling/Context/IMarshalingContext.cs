using BinaryFile.Marshaling.Common;
using BinaryFile.Marshaling.MarshalingStore;

namespace BinaryFile.Marshaling.Context
{
    public interface IMarshalingContext
    {
        string FieldName { get; }
        IMarshalerStore MarshalerStore { get; }
        int FieldAbsoluteOffset { get; }
        int ItemAbsoluteOffset { get; }
        int? ItemOffset { get; }
        int? FieldLength { get; }
        int? ItemLength { get; }
        Memory<byte> ItemSlice(Memory<byte> source);
        IMarshalingContext FindRelation(OffsetRelation offsetRelation);
        IMarshalingMetadata Metadata { get; }

        IMarshalingContext WithFieldByteLength(int? byteLength);
        IMarshalingContext WithItemByteLength(int? itemByteLength);
    }
}
