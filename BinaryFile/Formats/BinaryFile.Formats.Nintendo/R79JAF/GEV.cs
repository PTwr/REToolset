using BinaryDataHelper;
using BinaryFile.Unpacker.Marshalers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo.R79JAF
{
    public class EVE { }
    public class GEV
    {
        public static FluentMarshaler<GEV> PrepMarshaler()
        {
            var marshaler = new FluentMarshaler<GEV>();

            marshaler
                .WithField<string>("Magic")
                .AtOffset(0)
                .WithNullTerminator(false)
                .WithLengthOf(8)
                .WithExpectedValueOf(MagicNumber)
                .Into((gev, x) => gev.Magic = x)
                .From(root => GEV.MagicNumber);

            marshaler
                .WithField<int>("EVEBlockCount")
                .AtOffset(8 + 4 * 0)
                .Into((gev, x) => gev.EVEBlockCount = x)
                .From(gev => gev.EVEBlockCount);
            marshaler
                .WithField<int>("Alignment")
                .AtOffset(8 + 4 * 1)
                .WithExpectedValueOf(0x20)
                .Into((gev, x) => gev.Alignment = x)
                .From(gev => 0x20);
            marshaler
                .WithField<int>("OFSDataCount")
                .AtOffset(8 + 4 * 2)
                .Into((gev, x) => gev.OFSDataCount = x)
                .From(gev => gev.OFSDataCount);
            marshaler
                .WithField<int>("OFSDataOffset")
                .AtOffset(8 + 4 * 3)
                .Into((gev, x) => gev.OFSDataOffset = x)
                .From(gev => gev.OFSDataOffset);
            marshaler
                .WithField<int>("STRDataOffset")
                .AtOffset(8 + 4 * 4)
                .Into((gev, x) => gev.STRDataOffset = x)
                .From(gev => gev.STRDataOffset);

            marshaler
                .WithField<string>("EVE Magic")
                .AtOffset(0x1C)
                .WithNullTerminator(false)
                .WithLengthOf(4)
                .WithExpectedValueOf(EVEMagic)
                //TODO Allow Validation without Into?
                .Into((gev, x) => { })
                .From(root => GEV.EVEMagic);

            marshaler
                .WithField<byte[]>("EVE placeholder")
                .AtOffset(0x1C + 4) //header length
                .WithLengthOf(gev => gev.OFSDataOffset - 4 - 0x1C - 4) //between header and OFS, skipping magics
                .Into((gev, x) => gev.EVE = x)
                .From(gev => gev.EVE);

            marshaler
                .WithField<string>("OFS Magic")
                .AtOffset(eve => eve.OFSDataOffset - 4) //this points to OFS data, skipping magic
                .WithNullTerminator(false)
                .WithLengthOf(4)
                .WithExpectedValueOf(OFSMagic)
                //TODO Allow Validation without Into?
                .Into((gev, x) => { })
                .From(root => GEV.OFSMagic);

            marshaler
                .WithCollectionOf<ushort>("OFS placeholder")
                .AtOffset(eve => eve.OFSDataOffset)
                //.WithLengthOf(gev => gev.STRDataOffset - gev.OFSDataOffset - 4)
                .WithCountOf(gev => gev.OFSDataCount)
                .Into((gev, x) => gev.OFS = x.ToArray())
                .From(gev => gev.OFS);

            marshaler
                .WithField<string>("STR Magic")
                .AtOffset(eve => eve.STRDataOffset - 4) //this points to STR data, skipping magic
                .WithNullTerminator(false)
                .WithLengthOf(4)
                .WithExpectedValueOf(STRMagic)
                //TODO Allow Validation without Into?
                .Into((gev, x) => { })
                .From(root => GEV.STRMagic);

            marshaler
                .WithCollectionOf<string>("STR placeholder")
                .AtOffset(eve => eve.STRDataOffset)
                //.WithItemByteAlignment(4)
                .WithItemNullPadToAlignment(4)
                .WithNullTerminator(true)
                .WithEncoding(BinaryStringHelper.Shift_JIS)
                //no length, this goes until the end
                .Into((gev, x) => gev.STR = x.ToArray())
                .From(gev => gev.STR);

            return marshaler;
        }

        public const string MagicNumber = "$EVFEV02";
        public const string EVEMagic = "$EVE";
        public const string OFSMagic = "$OFS";
        public const string STRMagic = "$STR";
        public string Magic { get; private set; } = "$EVFEV02";

        public int EVEBlockCount { get; private set; }
        public int Alignment { get; private set; }
        public int OFSDataCount { get; private set; }
        //points to OFS content, skipping $OFS header
        public int OFSDataOffset { get; private set; }
        public int STRDataOffset { get; private set; }

        public byte[] EVE { get; set; }
        public ushort[] OFS { get; set; }
        public string[] STR { get; set; }
    }
}
