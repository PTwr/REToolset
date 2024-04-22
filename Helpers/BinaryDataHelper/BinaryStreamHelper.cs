using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryDataHelper
{
    public static class BinaryStreamHelper
    {
        public static bool Matches(this Span<byte> data, Span<byte?> pattern, Index index)
        {
            var patternPos = index.GetOffset(data.Length);

            //can't fit
            if (data.Length < patternPos + pattern.Length) return false;

            for (int i = 0; i < pattern.Length; i++)
            {
                //null is wildcard
                if (pattern[i] == null) continue;

                if (data[patternPos + i] != pattern[i]) return false;
            }

            return true;
        }

        public static bool StartsWith(this Span<byte> data, Span<byte?> pattern)
        {
            return data.Matches(pattern, new Index(0, false));
        }
        public static bool EndsWith(this Span<byte> data, Span<byte?> pattern)
        {
            return data.Matches(pattern, new Index(1, true));
        }
    }
}
