using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryDataHelper
{
    public static class AlignmentHelper
    {
        public static int Align(this int value, int alignmentInBytes, out int paddedby)
        {
            paddedby = 0;
            if (value % alignmentInBytes == 0) return value;

            paddedby = alignmentInBytes - (value % alignmentInBytes);
            return (1 + value / alignmentInBytes) * alignmentInBytes;

            var misalignedBy = value % alignmentInBytes;
            var alignment = (alignmentInBytes - misalignedBy) % alignmentInBytes;
            return value + alignment;
        }

        public static int Align(this int value, int alignmentInBytes)
        {
            if (value % alignmentInBytes == 0) return value;
            return (1 + value / alignmentInBytes) * alignmentInBytes;

            var misalignedBy = value % alignmentInBytes;
            var alignment = (alignmentInBytes - misalignedBy) % alignmentInBytes;
            return value + alignment;
        }
        public static IEnumerable<byte> PadRight(this IEnumerable<byte> bytes, int count, byte padValue = 0)
        {
            return bytes.Concat(Enumerable.Repeat(padValue, count));
        }
        public static IEnumerable<byte> PadLeft(this IEnumerable<byte> bytes, int count, byte padValue = 0)
        {
            return Enumerable.Repeat(padValue, count).Concat(bytes);
        }

        public static IEnumerable<byte> PadToAlignment(this IEnumerable<byte> bytes, int alignmentBytes)
        {
            return bytes.PadToAlignment(alignmentBytes, out _);
        }

        public static IEnumerable<byte> PadToAlignment(this IEnumerable<byte> bytes, int alignmentBytes, out int paddedBy)
        {
            var misalignedBy = bytes.Count() % alignmentBytes;
            paddedBy = (alignmentBytes - misalignedBy) % alignmentBytes;

            return bytes.PadRight(paddedBy, 0);
        }
    }
}
