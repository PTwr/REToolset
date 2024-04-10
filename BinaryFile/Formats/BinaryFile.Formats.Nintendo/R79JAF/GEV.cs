using BinaryDataHelper;
using BinaryFile.Unpacker.Marshalers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo.R79JAF
{
    public class EVEJumpTable : EVEBlock
    {
        public EVEJumpTable(EVESegment parent) : base(parent)
        {
        }
    }

    public class EVESegment
    {
        public EVESegment(GEV parent)
        {
            Parent = parent;
        }
        public static FluentMarshaler<EVESegment> PrepMarshaler()
        {
            var marshaler = new FluentMarshaler<EVESegment>();

            marshaler
                .WithField<string>("Magic")
                .AtOffset(0)
                .WithExpectedValueOf(GEV.EVEMagic)
                .WithLengthOf(GEV.EVEMagic.Length)
                .Into((eve, x) => eve.EVEMagic = x)
                .From(eve => GEV.EVEMagic);

            marshaler
                .WithCollectionOf<EVEBlock>("EVE Lines")
                .AtOffset(GEV.EVEMagic.Length)
                .BreakWhen((obj, items, data, ctx) =>
                {
                    //stop reading lines if next opcode is EVE terminator
                    return (
                        ctx.Slice(data)[0] == 0x00 &&
                        ctx.Slice(data)[1] == 0x06 &&
                        ctx.Slice(data)[2] == 0xFF &&
                        ctx.Slice(data)[3] == 0xFF);
                })
                .WithItemLengthOf((eve, block) => (block.EVELines.Sum(l => l.LineOpCodeCount) + 1) * 4)
                .Into((eve, x) => eve.Blocks = x.ToList())
                .From(eve => eve.Blocks);

            marshaler
                .WithField<EVEOpCode>("EVE Terminator")
                .WithValidator((block, terminator) => terminator.Instruction == 0x00006 && terminator.Parameter == 0xFFFF)
                .AtOffset(eve => (eve.Blocks.Sum(b => b.EVELines.Sum(i => i.LineOpCodeCount) + 1) + 1) * 4)
                .From(eve => eve.Terminator)
                .Into((eve, x) => eve.Terminator = x);

            return marshaler;
        }

        //$EVE
        public string EVEMagic { get; set; }
        public List<EVEBlock> Blocks { get; set; }
        //0006FFFF
        public EVEOpCode Terminator { get; set; }
        public GEV Parent { get; }
    }
    public class EVEBlock
    {
        public EVEBlock(EVESegment parent)
        {
            Parent = parent;
        }
        public static FluentMarshaler<EVEBlock> PrepMarshaler()
        {
            var marshaler = new FluentMarshaler<EVEBlock>();

            marshaler
                .WithCollectionOf<EVELine>("EVE Lines")
                .AtOffset(0)
                .BreakWhen((obj, items, data, ctx) =>
                {
                    //stop reading lines if next opcode is block terminator
                    return (
                        ctx.Slice(data)[0] == 0x00 &&
                        ctx.Slice(data)[1] == 0x05 &&
                        ctx.Slice(data)[2] == 0xFF &&
                        ctx.Slice(data)[3] == 0xFF);
                })
                .WithItemLengthOf((block, line) => line.LineOpCodeCount * 4)
                .Into((block, x) => block.EVELines = x.ToList())
                .From(block => block.EVELines);

            marshaler
                .WithField<EVEOpCode>("Block Terminator")
                .WithValidator((block, terminator) => terminator.Instruction == 0x00005 && terminator.Parameter == 0xFFFF)
                .AtOffset(block => block.EVELines.Sum(i => i.LineOpCodeCount) * 4)
                .From(block => block.Terminator)
                .Into((block, x) => block.Terminator = x);

            return marshaler;
        }

        //no way to tell EVELine count before parsing? Gotta lurk for Terminator?
        public List<EVELine> EVELines { get; set; }
        //0005FFFF
        public EVEOpCode Terminator { get; set; }
        public EVESegment Parent { get; }
    }
    public class EVELine
    {
        public EVELine(EVEBlock parent)
        {
            Parent = parent;
        }
        public static FluentMarshaler<EVELine> PrepMarshaler()
        {
            var marshaler = new FluentMarshaler<EVELine>();

            marshaler
                .WithField<EVEOpCode>("LineStart")
                .AtOffset(0)
                .WithValidator((line, opcode) => opcode.Instruction == 0x00001)
                .From(line => line.LineStartOpCode)
                .Into((line, x) => line.LineStartOpCode = x);
            marshaler
                .WithField<EVEOpCode>("LineLengthOpCode")
                .AtOffset(4)
                .From(line => line.LineLengthOpCode)
                .Into((line, x) => line.LineLengthOpCode = x);

            marshaler
                .WithCollectionOf<EVEOpCode>("Body")
                .InDeserializationOrder(10) //has to be after LineLength
                .AtOffset(8)
                .WithItemLengthOf(4)
                //TODO split count and length into deserialization and serialization?
                .WithCountOf(line => line.Body?.Count ?? line.BodyOpCodeCount)
                .From(line => line.Body)
                .Into((line, x) => line.Body = x.ToList());

            marshaler
                .WithField<EVEOpCode>("Line Terminator")
                .WithValidator((block, terminator) => terminator.Instruction == 0x00004 && terminator.Parameter == 0x0000)
                .AtOffset(line => line.LineOpCodeCount * 4 - 4)
                .From(line => line.Terminator)
                .Into((line, x) => line.Terminator = x);

            return marshaler;
        }

        //Expected Instruction = 1
        public EVEOpCode LineStartOpCode { get; set; }
        public ushort LineId => LineStartOpCode.Parameter;

        //TODO analyze Param -> unknown, some kind of line type, or maybe nesting? Appears to be same in similar lines
        //Param = 0002 -> first line of block? but following lines can have either 03 or 05
        public EVEOpCode LineLengthOpCode { get; set; }
        public int LineOpCodeCount => LineLengthOpCode.Instruction;
        public int BodyOpCodeCount => LineLengthOpCode.Instruction - 3; //without Start, Length, and Terminator, opcodes

        public List<EVEOpCode> Body { get; set; }

        //00040000
        public EVEOpCode Terminator { get; set; }
        public EVEBlock Parent { get; }
    }

    //TODO implicit converters to compare with 32bit hex mask?
    public class EVEOpCode
    {
        //TODO rethink Terminator, just uint32 should be enough and won't fuck up hierarchy.
        //TODO same with Line header! Hierarchy doesnt work so well if class is used on multiple levels
        public EVEOpCode() { }
        public EVEOpCode(EVESegment parent)
        {
            ParentSegment = parent;
        }
        public EVEOpCode(EVEBlock parent)
        {
            ParentBlock = parent;
        }
        public EVEOpCode(EVELine parent)
        {
            ParentLine = parent;
        }
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
        public EVELine? ParentLine { get; }
        public EVEBlock? ParentBlock { get; }
        public EVESegment? ParentSegment { get; }
    }
    public class EVE { }
    public class GEV
    {
        public List<EVEOpCode> EVEOpCodes { get; set; }
        public EVESegment EVESegment { get; set; }

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
                ;//.From(gev => gev.EVEOpCodes)
                //.Into((gev, x) => gev.EVEOpCodes = x.ToList());

            marshaler
                .WithField<EVESegment>("EVE Segment")
                .AtOffset(0x1C)
                .InDeserializationOrder(10)
                .Into((gev, eve) => gev.EVESegment = eve)
                .From(gev=>gev.EVESegment);

            marshaler
                .WithField<string>("Magic")
                .AtOffset(0)
                .WithNullTerminator(false)
                .WithLengthOf(8)
                .WithExpectedValueOf(MagicNumber)
                .Into((gev, x) => gev.Magic = x)
                .From(root => GEV.MagicNumber);

            marshaler
                .WithField<int>("EVELineCount")
                .AtOffset(8 + 4 * 0)
                .Into((gev, x) => gev.EVELineCount = x)
                .From(gev => gev.EVELineCount);
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
                                                                       //.Into((gev, x) => gev.EVE = x)
                                                                       //.From(gev => gev.EVE);
                ;

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

        public int EVELineCount { get; private set; }
        public int Alignment { get; private set; }
        public int OFSDataCount { get; private set; }
        //points to OFS content, skipping $OFS header
        public int OFSDataOffset { get; private set; }
        public int STRDataOffset { get; private set; }

        public byte[] EVEBytes { get; set; }

        public Dictionary<int, ushort> OFS { get; set; }
        public List<string> STR { get; set; }
    }
}
