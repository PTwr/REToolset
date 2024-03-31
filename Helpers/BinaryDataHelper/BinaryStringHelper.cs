using System.Text;

namespace BinaryDataHelper
{
    public static class BinaryStringHelper
    {
        static BinaryStringHelper()
        {
            //inits System.Text.Encoding.CodePages package to access more codepages
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private static byte[] NullTerminator = new byte[] { 0 };

        public static Span<byte> FindNullTerminator(this Span<byte> buffer, int start = 0)
        {
            return buffer.FindNullTerminator(out _, start);
        }
        public static Span<byte> FindNullTerminator(this Span<byte> buffer, out bool noTerminator, int start = 0)
        {
            buffer = start > 0 ? buffer.Slice(start) : buffer;
            int l = 0;
            while (l < buffer.Length && buffer[l] != 0)
            {
                l++;
            }

            noTerminator = buffer[l] is not 0;

            return buffer.Slice(0, l);
        }
        public static Span<byte> FindNullTerminatedString(this Span<byte> buffer, int start = 0)
        {
            return FindNullTerminator(buffer, start);
        }

        public static string ToDecodedString(this Span<byte> buffer, Encoding encoding)
        {
            return encoding.GetString(buffer);
        } 

        public static byte[] ToBytes(this string text, Encoding encoding, bool appendNullTerminator = false, int fixedLength = -1)
        {
            var bytes = encoding.GetBytes(text);

            if (appendNullTerminator)
            {
                return bytes.Concat(NullTerminator).ToArray();
            }

            if (fixedLength >= 0)
            {
                if (bytes.Length > fixedLength)
                {
                    throw new Exception($"Byte representation is longer than fixed length. ${bytes.Length} > {fixedLength}");
                }
                if (bytes.Length < fixedLength)
                {
                    bytes = bytes.PadToAlignment(fixedLength).ToArray();
                }
            }

            return bytes.ToArray();
        }

        private static Encoding? shiftJIS;
        public static Encoding Shift_JIS
        {
            get
            {
                if (shiftJIS != null)
                {
                    return shiftJIS;
                }

                shiftJIS = Encoding.GetEncoding("shift_jis");
                return shiftJIS;
            }
        }

        private static Encoding? w1250;
        public static Encoding Windows1250
        {
            get
            {
                if (w1250 != null)
                {
                    return w1250;
                }

                //inits System.Text.Encoding.CodePages package to access more codepages
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                w1250 = Encoding.GetEncoding(1250);
                return w1250;
            }
        }
    }
}
