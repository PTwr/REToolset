using BinaryFile.MarshalingDI.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Tests
{
    public class OffsetStackTests
    {
        [Fact]
        public void BasicTest()
        {
            IOffsetStack stack = new DefaultOffsetStack();

            //absolute root
            Assert.Equal(0, stack.CurrentAbsoluteOffset);

            //only -1 (absolute) is acceptable negative value
            Assert.Throws<ArgumentException>(() => stack.Push(-2, (OffsetRelation)(-7)));


            //dont allow absolute offset to get into negatives
            Assert.Throws<IndexOutOfRangeException>(() => stack.Push(-2, OffsetRelation.Segment));
            Assert.Throws<IndexOutOfRangeException>(() => stack.CalculateAbsoluteOffset(-2, OffsetRelation.Segment));

            //dont allow relation to offsets not on stack
            Assert.Throws<IndexOutOfRangeException>(() => stack.CalculateAbsoluteOffset(1, OffsetRelation.Parent));
            Assert.Throws<IndexOutOfRangeException>(() => stack.CalculateAbsoluteOffset(2, OffsetRelation.GrandParent));

            //+0
            Assert.Equal(0, stack.CalculateAbsoluteOffset(0, OffsetRelation.Segment));
            //+1
            Assert.Equal(1, stack.CalculateAbsoluteOffset(1, OffsetRelation.Segment));
            //+2
            Assert.Equal(2, stack.CalculateAbsoluteOffset(2, OffsetRelation.Segment));

            //reset to 10
            stack.Push(10, OffsetRelation.Absolute);

            Assert.Equal(10, stack.CurrentAbsoluteOffset);
            //+2
            Assert.Equal(12, stack.CalculateAbsoluteOffset(2, OffsetRelation.Segment));
            //-2
            Assert.Equal(8, stack.CalculateAbsoluteOffset(-2, OffsetRelation.Segment));

            //0+3
            Assert.Equal(3, stack.CalculateAbsoluteOffset(3, OffsetRelation.Parent));

            //10+2
            stack.Push(2, OffsetRelation.Segment);

            Assert.Equal(12, stack.CurrentAbsoluteOffset);

            stack.SetOffsetShift(7);
            Assert.Equal(12 + 7, stack.CurrentAbsoluteOffset);
            stack.AddOffsetShift(-7);
            Assert.Equal(12, stack.CurrentAbsoluteOffset);

            //reset to 2
            stack.Push(2, OffsetRelation.Absolute);

            Assert.Equal(2, stack.CurrentAbsoluteOffset);

            //2+2
            stack.Push(2, OffsetRelation.Segment);

            Assert.Equal(4, stack.CurrentAbsoluteOffset);

            //2+2+2
            stack.Push(2, OffsetRelation.Segment);

            Assert.Equal(6, stack.CurrentAbsoluteOffset);

            //2+2+1
            stack.Push(1, OffsetRelation.Parent);

            Assert.Equal(5, stack.CurrentAbsoluteOffset);
        }
    }
}
