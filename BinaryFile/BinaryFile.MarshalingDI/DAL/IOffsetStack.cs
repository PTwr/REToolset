namespace BinaryFile.MarshalingDI.DAL
{
    public interface IOffsetStack
    {
        void Pop(int count = 1);
        void Push(int offset, OffsetRelation offsetRelation, string tag = "");

        int CurrentAbsoluteOffset { get; }
        int CalculateAbsoluteOffset(int relativeOffset, OffsetRelation offsetRelation, string tag = "");
        void SetOffsetShift(int shift);
        void AddOffsetShift(int shift);
    }
}
