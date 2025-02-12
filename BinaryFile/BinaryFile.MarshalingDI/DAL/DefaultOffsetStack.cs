namespace BinaryFile.MarshalingDI.DAL
{
    public class DefaultOffsetStack : IOffsetStack
    {
        //simplify parenthood math by ensuring there always is an absolute root parent to start with
        private List<(int absoluteOffset, string tag)> stack = [(0, "root")];
        private int offsetShift = 0;

        public DefaultOffsetStack()
        {

        }

        public int CurrentAbsoluteOffset => stack.Last().absoluteOffset + offsetShift;

        public int CalculateAbsoluteOffset(int relativeOffset, OffsetRelation offsetRelation, string tag = "")
        {
            if (offsetRelation == OffsetRelation.Absolute) return relativeOffset;
            if (offsetRelation < OffsetRelation.Absolute) throw new ArgumentException($"Unsupported {nameof(OffsetRelation)} of {offsetRelation.ToString()}. Tag: '{tag}'");

            int stackId = stack.Count - (int)offsetRelation - 1;
            if (stackId < 0) throw new IndexOutOfRangeException($"{nameof(OffsetRelation)} of {offsetRelation} peeks below root. Stack depth: {stack.Count}. Tag: '{tag}'");

            var result = stack[stackId].absoluteOffset + relativeOffset + offsetShift;
            if (result < 0) throw new IndexOutOfRangeException($"Calculated negative offset of '{result}' from {stack[stackId]}+{relativeOffset} with shift of '{offsetShift}' and relation of '{offsetRelation}'. Tag: '{tag}'");
            return result;
        }

        public void Pop(int count = 1)
        {
            if (stack.Count < count)
                throw new Exception($"Popping more levels ({count}) than currently on stack ({stack.Count})");

            stack.RemoveAt(stack.Count - 1);
        }

        public void Push(int offset, OffsetRelation offsetRelation, string tag = "")
        {
            stack.Add((CalculateAbsoluteOffset(offset, offsetRelation, tag), tag));
        }

        public void SetOffsetShift(int shift)
        {
            this.offsetShift = shift;
        }
        public void AddOffsetShift(int shift)
        {
            this.offsetShift += shift;
        }
    }
}
