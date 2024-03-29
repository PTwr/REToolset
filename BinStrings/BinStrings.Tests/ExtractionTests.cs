using BinaryDataHelper;
using BinStrings.Lib;
using System.Text;
using System.Text.RegularExpressions;

namespace BinStrings.Tests
{
    public class ExtractionTests
    {
        [Fact]
        public void Lupin()
        {
            var data = File.ReadAllBytes(@"C:\Users\User\Documents\lupin\1clean\files\COMMON\sfil.BIN");

            var recodedString = data.AsSpan().ToDecodedString(BinaryStringHelper.Shift_JIS);

            var results = RegexpExtractor.FindStrings(
                new Regex(@"(?<stringopcode>3)(?<length>[\x01-\xFF])(?<text>[\u0001-\uFFFF]+)(?<nullterminator>\x00)", RegexOptions.Compiled), 
                data,
                BinaryStringHelper.Shift_JIS);

            var x = results.ToList();

            var distinctStrings = x.Select(i => i.OriginalText).Distinct().ToList();

        }
    }
}