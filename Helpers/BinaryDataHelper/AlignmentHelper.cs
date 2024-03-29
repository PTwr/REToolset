using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryDataHelper
{
    public static class AlignmentHelper
    {
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
