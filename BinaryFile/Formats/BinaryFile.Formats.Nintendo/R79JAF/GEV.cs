using BinaryDataHelper;
using BinaryFile.Unpacker.Marshalers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo.R79JAF
{
    public class EVEOpCode
    {
        public static FluentMarshaler<EVEOpCode> PrepMarshaler()
        {
            var marshaler = new FluentMarshaler<EVEOpCode>();

            marshaler
                .WithField<ushort>("Instructin")
                .AtOffset(0)
                .From(opcode => opcode.Instruction)
                .Into((opcode, x) => opcode.Instruction = x);
            marshaler
                .WithField<ushort>("Parameter")
                .AtOffset(2)
                .From(opcode => opcode.Parameter)
                .Into((opcode, x) => opcode.Parameter = x);

            return marshaler;
        }

        public override string ToString()
        {
            return $"{Instruction:X4} {Parameter:X4}";
        }

        public ushort Instruction { get; set; }
        public ushort Parameter { get; set; }
    }
    public class EVE { }
    public class GEV
    {
        public List<EVEOpCode> EVEOpCodes { get; set; }

        public static FluentMarshaler<GEV> PrepMarshaler()
        {
            var marshaler = new FluentMarshaler<GEV>();

            marshaler
                .WithCollectionOf<EVEOpCode>("OpCodes")
                .AtOffset(0x1C + GEV.EVEMagic.Length)
                .InDeserializationOrder(10) //after header is read
                .WithItemLengthOf(4)
                .WithLengthOf(gev =>
                {
                    return gev.EVEOpCodes?.Count * 4 ?? (gev.OFSDataOffset - GEV.OFSMagic.Length - 0x1C - GEV.EVEMagic.Length);
                })
                .From(gev => gev.EVEOpCodes)
                .Into((gev, x) => gev.EVEOpCodes = x.ToList());

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
                .WithField<int>("Alignment") //TODO test, some GEV's might have nullpadding based on this value
                .AtOffset(8 + 4 * 1)
                .WithExpectedValueOf(0x20)
                .Into((gev, x) => gev.Alignment = x)
                .From(gev => 0x20);
            marshaler
                .WithField<int>("OFSDataCount")
                .AtOffset(8 + 4 * 2)
                .Into((gev, x) => gev.OFSDataCount = x)
                .InSerializationOrder(150) //between STR and OFS serialization
                .From(gev => gev.OFSDataCount = gev.OFS.Count);
            marshaler
                .WithField<int>("OFSDataOffset")
                .AtOffset(8 + 4 * 3)
                .Into((gev, x) => gev.OFSDataOffset = x)
                .From(gev => gev.OFSDataOffset); //TODO calculate from EVE length
            marshaler
                .WithField<int>("STRDataOffset")
                .AtOffset(8 + 4 * 4)
                .Into((gev, x) => gev.STRDataOffset = x)
                //TODO after OFSDataOffset gets recalculated from EVE
                .From(gev => gev.STRDataOffset = gev.OFSDataOffset + gev.STR.Count() * 2 + GEV.STRMagic.Length);

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

            //TODO conditional deserializatoin, this magic is optional
            marshaler
                .WithField<string>("STR Magic")
                .AtOffset(eve => eve.STRDataOffset - 4) //this points to STR data, skipping magic
                .WithNullTerminator(false)
                .WithLengthOf(4)
                .WithExpectedValueOf(STRMagic)
                //TODO Allow Validation without Into?
                .Into((gev, x) => { })
                .From(root => GEV.STRMagic);

            //TODO conditional deserializatoin, this section is optional
            marshaler
                .WithCollectionOf<string>("STR placeholder")
                .InSerializationOrder(100)
                .AtOffset(eve => eve.STRDataOffset)
                .WithItemNullPadToAlignment(4)
                .WithNullTerminator(true)
                .WithEncoding(BinaryStringHelper.Shift_JIS)
                //no length, this goes until the end
                .Into((gev, x) => gev.STR = x.ToList())
                .From(gev => gev.STR)
                .AfterSerializing((GEV gev, string item, int n, int itemByteLength, int itemOffset) =>
                {
                    //OFS holds index to 16bit (4byte) offsets in STR
                    gev.OFS[n] = (ushort)(itemOffset >> 2);
                });

            marshaler
                .WithField<string>("OFS Magic")
                .AtOffset(eve => eve.OFSDataOffset - 4) //this points to OFS data, skipping magic
                .WithNullTerminator(false)
                .WithLengthOf(4)
                .WithExpectedValueOf(OFSMagic)
                //TODO Allow Validation without Into?
                .Into((gev, x) => { })
                .InSerializationOrder(200) //after STR, awaiting for offset update
                .From(root => GEV.OFSMagic);

            //TODO conditional deserializatoin, this section is optional (but header stays)
            marshaler
                .WithCollectionOf<ushort>("OFS")
                .AtOffset(eve => eve.OFSDataOffset)
                .WithCountOf(gev => gev.OFSDataCount)
                .Into((gev, x) => gev.OFS = x.Select((x, n) => new { x, n }).ToDictionary(x => x.n, x => x.x))
                .InSerializationOrder(200) //after STR, awaiting for offset update
                .From(gev => gev.OFS.Values.AsEnumerable());

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
        public Dictionary<int, ushort> OFS { get; set; }
        public List<string> STR { get; set; }
    }
}
