using System.Text;
using System.Text.RegularExpressions;
using BinaryDataHelper;

namespace BinStrings.Lib
{
    public class BinStringEntry
    {
        public int Index;
        public int Offset;
        public int ByteLength;
        public string? OriginalText;
        public string? Replacement;

        public override string ToString()
        {
            return OriginalText ?? "";
        }
    }
    public static class RegexpExtractor
    {
        public static IEnumerable<BinStringEntry> FindStrings(Regex regex, byte[] data, Encoding encoding)
        {
            var recodedString = data.AsSpan().ToDecodedString(encoding);

            int index = 0;
            foreach (Match match in regex.Matches(recodedString))
            {
                //prioritize text group
                var txt = match.Groups.ContainsKey("text") ? match.Groups["text"] : match.Groups[0];

                var origBytes = txt.Value.ToBytes(encoding);

                //count original bytes in everything preceeding match
                var pos = recodedString
                    .Substring(0, txt.Index)
                    .ToBytes(encoding)
                    .Length;

                var entry = new BinStringEntry()
                {
                    Index = index,
                    Offset = pos,
                    ByteLength = origBytes.Length,
                    OriginalText = txt.Value,
                    Replacement = txt.Value,
                };

                yield return entry;

                index++;
            }
        }
    }
}
