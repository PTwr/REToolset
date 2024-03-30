namespace BinaryFile.Unpacker.Metadata
{
    public enum OffsetRelation : int
    {
        Absolute = -1,
        Segment = 0,
        Parent = 1,
        GrandParent = 2,
    }
}
