namespace BinaryFile.MarshalingDI.DAL
{
    public class DefaultOffsetStack : IOffsetStack
    {
        private class OffsetStackItem
        {
            public OffsetStackItem(int absoluteOffset, int offsetShift, string tag)
            {
                this.AbsoluteOffset = absoluteOffset;
                this.OffsetShift = offsetShift;
                this.Tag = tag;
            }

            public int AbsoluteOffset;
            public int OffsetShift;
            public string Tag;

            public override string ToString()
            {
                return $"{AbsoluteOffset}+{OffsetShift}={AbsoluteOffset+OffsetShift}";
            }
        }

        //simplify parenthood math by ensuring there always is an absolute root parent to start with
        private List<OffsetStackItem> stack = [new OffsetStackItem(0, 0, "root")];

        public DefaultOffsetStack()
        {

        }

        public int CurrentAbsoluteOffset => stack.Last().AbsoluteOffset + stack.Last().OffsetShift;

        public int CalculateAbsoluteOffset(int relativeOffset, OffsetRelation offsetRelation, string tag = "")
        {
            if (offsetRelation == OffsetRelation.Absolute) return relativeOffset;
            if (offsetRelation < OffsetRelation.Absolute) throw new ArgumentException($"Unsupported {nameof(OffsetRelation)} of {offsetRelation.ToString()}. Tag: '{tag}'");

            int stackId = stack.Count - (int)offsetRelation - 1;
            if (stackId < 0) throw new IndexOutOfRangeException($"{nameof(OffsetRelation)} of {offsetRelation} peeks below root. Stack depth: {stack.Count}. Tag: '{tag}'");

            var result = stack[stackId].AbsoluteOffset + stack[stackId].OffsetShift + relativeOffset;
            if (result < 0) throw new IndexOutOfRangeException($"Calculated negative offset of '{result}' from {stack[stackId]}+{relativeOffset} with relation of '{offsetRelation}'. Tag: '{tag}'");
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
            stack.Add(new OffsetStackItem(CalculateAbsoluteOffset(offset, offsetRelation, tag), 0, tag));
        }

        public void SetOffsetShift(int shift)
        {
            stack.Last().OffsetShift = shift;
        }
        public void AddOffsetShift(int shift)
        {
            stack.Last().OffsetShift += shift;
        }
    }
}
