using BinaryFile.Unpacker.Metadata;
using System.Text;

namespace BinaryFile.Unpacker.New.Interfaces
{
    //TODO implement :)
    public interface IMarshalingContext
    {
        string FieldName { get; }
        IMarshalerStore MarshalerStore { get; }
        int FieldAbsoluteOffset { get; }
        int ItemAbsoluteOffset { get; }
        int? ItemOffset { get; }
        int? FieldLength { get; }
        int? ItemLength { get; }
        Span<byte> ItemSlice(Span<byte> source);
        IMarshalingContext FindRelation(OffsetRelation offsetRelation);
        IMarshalingMetadata Metadata { get; }

        IMarshalingContext WithFieldByteLength(int? byteLength);
        IMarshalingContext WithItemByteLength(int? itemByteLength);
    }

    /// <summary>
    /// optional Metadata not necessary for calculating Offset or Slicing
    /// </summary>
    public interface IMarshalingMetadata
    {
        Encoding Encoding { get; }
        bool LittleEndian { get; }
        bool NullTerminated { get; }
        int? ItemCount { get; }
    }
}
