using BinaryFile.Unpacker.Metadata;

namespace BinaryFile.Unpacker.New.Interfaces
{
    //TODO implement :)
    public interface IMarshalingContext
    {
        IMarshalerStore MarshalerStore { get; }
        int FieldAbsoluteOffset { get; }
        int ItemAbsoluteOffset { get; }
        int? ItemOffset { get; }
        int? FieldLength { get; }
        int? ItemLength { get; }
        Span<byte> ItemSlice(Span<byte> source);
        IMarshalingContext FindRelation(OffsetRelation offsetRelation);
    }
}
