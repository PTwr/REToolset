using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Data;
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
        public static bool StartsWith(this Span<byte> data, int magic)
        {
            byte[] bytes = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(), magic);

            return data.StartsWith(bytes);
        }
        public static bool EndsWith(this Span<byte> data, int magic)
        {
            byte[] bytes = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(), magic);

            return data.EndsWith(bytes);
        }
    }
}
