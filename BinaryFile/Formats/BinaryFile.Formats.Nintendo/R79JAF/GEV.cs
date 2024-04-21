using BinaryDataHelper;
using BinaryFile.Unpacker;
using BinaryFile.Unpacker.Marshalers;
using BinaryFile.Unpacker.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo.R79JAF
{
    public class EVEJumpTableEntry
    {
        public static new FluentMarshaler<EVEJumpTableEntry> PrepMarshaler()
        {
            var marshaler = new FluentMarshaler<EVEJumpTableEntry>();

            //TODO nicely named consts
            marshaler
                .WithField<ushort>("Jump Instruction")
                .AtOffset(0)
                //TODO add option to validate without needing empty Into
                .WithExpectedValueOf(0x0013)
                .Into((entry, x) => { })
                .From(entry => 0x0013);
            marshaler
                .WithField<ushort>("Padding")
                .AtOffset(6)
                //TODO add option to validate without needing empty Into
                .WithExpectedValueOf(0xFFFF)
                .Into((entry, x) => { })
                .From(entry => 0xFFFF);

            marshaler
                .WithField<ushort>("JumpOffset")
                .AtOffset(2)
                .Into((entry, x) => entry.JumpOffset = x)
                .From(entry => (ushort)entry.JumpOffset);
            marshaler
                .WithField<ushort>("Jump/Return Id")
                .AtOffset(4)
                .Into((entry, x) => entry.JumpId = x)
                .From(entry => (ushort)entry.JumpId);

            return marshaler;
        }
        public EVEJumpTableEntry(EVEJumpTableBlock parent)
        {
            Parent = parent;
        }

        public override string ToString()
        {
            return $"0x{JumpId:X2} -> 0x{JumpOffset:X2}";
        }

        //jump to 32bit aligned offset (opcode id), should be aligned with line starts
        public int JumpOffset { get; set; }
        //TODO validate, its possibly return label
        public int JumpId { get; set; }
        public EVEJumpTableBlock Parent { get; }
    }
    //TODO move raw OpCodes to RawEVEBlock? Or make base interface?
    //TODO JumpTable does not need raw OpCodes but header/footer and validation could be reused
    public class EVEJumpTableBlock : EVEBlock
    {
        private const int LineStartExpectedValue = 0x00010000;

        public static new FluentMarshaler<EVEJumpTableBlock> PrepMarshaler()
        {
            var marshaler = new FluentMarshaler<EVEJumpTableBlock>();

            marshaler
                .WithField<int>("Line Start")
                .AtOffset(0)
                .WithExpectedValueOf(LineStartExpectedValue) //only first Block is expected to be jump Table
                .Into((block, x) => { })
                .From(block => LineStartExpectedValue);

            marshaler
                .WithField<ushort>("Line length")
                .AtOffset(4)
                //jumpcount - 1
                .From(block => (ushort)(block.Jumps.Count * 2 + 6 - 1));

            marshaler
                .WithField<byte[]>("Jumptable header")
                .AtOffset(6)
                .From(i => Mask);

            marshaler
                .WithField<ushort>("Jump count")
                .AtOffset(14)
                .Into((block, x) => block.JumpCount = (x - 6) / 2)
                .From(block => (ushort)(block.Jumps.Count * 2 + 6));

            marshaler
                .WithCollectionOf<EVEJumpTableEntry>("Jump table")
                .AtOffset(16)
                .WithCountOf(block => block.JumpCount)
                .WithItemLengthOf(8) //TODO add WORD/DWORD/QWORD consts? Can be taken care of by adding Extension methods
                .From(block => block.Jumps)
                .WithValidator((block, items) =>
                {
                    //JumpId validation can be performed with validator
                    var sequential = items
                        .Select((item, n) => item.JumpId == n)
                        .All(i => i);

                    if (!sequential)
                        throw new Exception("Non sequential JumpId's!!");

                    if (items.First().JumpId != 0)
                        throw new Exception($"Non-zero jump id of first jump! Actual: {items.First().JumpId}!");

                    return true;
                })
                .Into((block, item, _, n, offset) =>
                {
                    //or during insertion
                    if (block.Jumps.Any() && block.Jumps.Last().JumpId != item.JumpId - 1)
                        throw new Exception($"Non sequential jump id's for jump {n} with id of {item.JumpId}!");
                    if (!block.Jumps.Any() && item.JumpId != 0)
                        throw new Exception($"Non-zero jump id of first jump! Actual: {item.JumpId}!");

                    block.Jumps.Add(item);
                });


            marshaler
                .WithField<EVEOpCode>("Lineend")
                .AtOffset(block => block.ByteLength - 4 - 4)
                .From(block => new EVEOpCode() { Instruction = 0x0004, Parameter = 0x0000 });
            marshaler
                .WithField<EVEOpCode>("Terminator")
                //TODO consts, or inherit from base class, or nullable byte patterns?
                .WithValidator((block, terminator) => terminator.Instruction == 0x00005 && terminator.Parameter == 0xFFFF)
                .AtOffset(block => block.ByteLength - 4)
                .From(block => block.Terminator)
                .Into((block, x) => block.Terminator = x);

            return marshaler;
        }

        public override int ByteLength => Jumps.Count * 8 + 5 * 4 + 4;

        public List<EVEJumpTableEntry> Jumps { get; set; } = new List<EVEJumpTableEntry>();

        public int JumpCount { get; set; }
        public static readonly byte[] Mask = [0x00, 0x02, 0x00, 0x03, 0x00, 0x00, 0x00, 0x14];
        public EVEJumpTableBlock(EVESegment parent) : base(parent)
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
                .WithExpectedValueOf(GEV.EVEMagicNumber)
                .WithLengthOf(GEV.EVEMagicNumber.Length)
                .Into((eve, x) => eve.EVEMagic = x)
                .From(eve => GEV.EVEMagicNumber);

            marshaler
                .WithCollectionOf<EVEBlock>("EVE Lines")
                .AtOffset(GEV.EVEMagicNumber.Length)
                //TODO this has to be rethought hard :)
                //generic contravariance prevents returning ISerializer<EVEJumpTableBlock>
                //adding Serialize(object obj) overload would make a mess of multi-tupe marshalers like Integers
                .WithCustomSerialization((EVESegment obj, EVEBlock item, ByteBuffer buffer, IMarshalingContext ctx, out int l) =>
                {
                    //ISerializer<EVEJumpTableBlock> a = null;
                    //ISerializer<EVEBlock> b = null;
                    //return (ISerializer<EVEBlock>)a;
                    //b = a;
                    //a = b;
                    if (item is EVEJumpTableBlock)
                    {
                        if (ctx.SerializerManager.TryGetMapping<EVEJumpTableBlock>(out var sJumpTable) is false || sJumpTable is null)
                            throw new Exception($"No mapping found for EVEJumpTableBlock!");
                        sJumpTable.Serialize((EVEJumpTableBlock)item, buffer, ctx, out l);
                        return;
                    }

                    //TODO handle defualt outsied?
                    //or maybe unify this abomination into default handling?
                    //derived map would then have to be built on top of base one
                    //but that would not work too well if type is derived but map not
                    //would need flag ot control fielddescriptor inheritance?
                    if (ctx.SerializerManager.TryGetMapping<EVEBlock>(out var sDefault) is false || sDefault is null)
                        throw new Exception($"No mapping found for EVeBlock!");
                    sDefault.Serialize(item, buffer, ctx, out l);
                })
                .WithCustomDeserializationMappingSelector((data, ctx) =>
                {
                    var itemSlice = ctx.Slice(data);
                    var determinator = itemSlice[1];

                    //TODO mapping slector helper which accepts byte?[] mask with optional offset for tested bytes?
                    if (itemSlice.Slice(6, 8).StartsWith(EVEJumpTableBlock.Mask.AsSpan()))
                    {
                        if (ctx.DeserializerManager.TryGetMapping<EVEJumpTableBlock>(out var dJumpTable) is false || dJumpTable is null)
                            throw new Exception($"No mapping found for EVEJumpTableBlock!");
                        return dJumpTable;
                    }

                    if (ctx.DeserializerManager.TryGetMapping<EVEBlock>(out var dDefault) is false || dDefault is null)
                        throw new Exception($"No mapping found for EVeBlock!");
                    return dDefault;
                })
                .BreakWhen((obj, items, data, ctx) =>
                {
                    //stop reading lines if next opcode is EVE terminator
                    return (
                        ctx.Slice(data)[0] == 0x00 &&
                        ctx.Slice(data)[1] == 0x06 &&
                        ctx.Slice(data)[2] == 0xFF &&
                        ctx.Slice(data)[3] == 0xFF);
                })
                .WithItemLengthOf((eve, block) => block.ByteLength)
                .Into((eve, x) => eve.Blocks = x.ToList())
                .From(eve => eve.Blocks);

            marshaler
                .WithField<EVEOpCode>("EVE Terminator")
                .WithValidator((block, terminator) => terminator.Instruction == 0x00006 && terminator.Parameter == 0xFFFF)
                .AtOffset(eve =>
                {
                    var terminatorOffset = eve.Blocks.Sum(b => b.ByteLength) + GEV.EVEMagicNumber.Length;
                    return terminatorOffset;
                })
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
        public virtual int ByteLength => (EVELines.Sum(l => l.LineOpCodeCount) + 1) * 4;
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
                .Into((line, x) => line.LineStartOpCode = x)
                //TODO add event on marshaler-level, this bit of logic does not need to be on field
                .AfterDeserializing((line, opcode, l, ctx) =>
                {
                    //pretty much an OpCode Id, offset to 32bit chunks since start of EVE body
                    line.JumpOffset = (ctx.AbsoluteOffset - 0x20) / 4;
                })
                ;
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

        /// <summary>
        /// id of opode in EVE (index of 32bit chunks)
        /// </summary>
        public int JumpOffset { get; set; }

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
                .AtOffset(0x1C + GEV.EVEMagicNumber.Length)
                .InDeserializationOrder(10) //after header is read
                .WithItemLengthOf(4)
                .WithLengthOf(gev =>
                {
                    return gev.EVEOpCodes?.Count * 4 ?? (gev.OFSDataOffset - GEV.OFSMagicNumber.Length - 0x1C - GEV.EVEMagicNumber.Length);
                })
                ;//.From(gev => gev.EVEOpCodes)
                 //.Into((gev, x) => gev.EVEOpCodes = x.ToList());

            marshaler
                .WithField<EVESegment>("EVE Segment")
                .AtOffset(0x1C)
                .InDeserializationOrder(10)
                .Into((gev, eve) => gev.EVESegment = eve)
                .From(gev => gev.EVESegment);

            marshaler
                .WithField<string>("Magic")
                .AtOffset(0)
                .WithNullTerminator(false)
                .WithLengthOf(8)
                .WithExpectedValueOf(GEVMagicNumber)
                .Into((gev, x) => gev.GEVMagic = x)
                .From(root => GEV.GEVMagicNumber);

            marshaler
                .WithField<int>("EVELineCount")
                .AtOffset(8 + 4 * 0)
                .Into((gev, x) => gev.EVELineCount = x)
                .From(gev => gev.EVELineCount);
            marshaler
                //TODO This is probably offset to EVE body, OFS and STR does that "skip the magic" thing as well!
                .WithField<int>("Alignment") //TODO test, some GEV's might have nullpadding based on this value
                .AtOffset(8 + 4 * 1)
                .WithExpectedValueOf(0x20)
                .Into((gev, x) => gev.EVEDataOffset = x)
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
                .From(gev => gev.STRDataOffset = gev.OFSDataOffset + gev.STR.Count() * 2 + GEV.STRMagicNumber.Length);

            marshaler
                .WithField<string>("EVE Magic")
                .AtOffset(0x1C)
                .WithNullTerminator(false)
                .WithLengthOf(4)
                .WithExpectedValueOf(EVEMagicNumber)
                //TODO Allow Validation without Into?
                .Into((gev, x) => { })
                .From(root => GEV.EVEMagicNumber);

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
                .WithExpectedValueOf(STRMagicNumber)
                //TODO Allow Validation without Into?
                .Into((gev, x) => { })
                .From(root => GEV.STRMagicNumber);

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
                .WithExpectedValueOf(OFSMagicNumber)
                //TODO Allow Validation without Into?
                .Into((gev, x) => { })
                .InSerializationOrder(200) //after STR, awaiting for offset update
                .From(root => GEV.OFSMagicNumber);

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

        public const string GEVMagicNumber = "$EVFEV02";
        public const string EVEMagicNumber = "$EVE";
        public const string OFSMagicNumber = "$OFS";
        public const string STRMagicNumber = "$STR";

        public string GEVMagic { get; set; } = GEVMagicNumber;
        public string EVEMagic { get; set; } = EVEMagicNumber;
        public string OFSMagic { get; set; } = OFSMagicNumber;
        public string STRMagic { get; set; } = STRMagicNumber;

        public int EVELineCount { get; set; }
        public int EVEDataOffset { get; set; }
        public int OFSDataCount { get; set; }
        //points to OFS content, skipping $OFS header
        public int OFSDataOffset { get; set; }
        public int STRDataOffset { get; set; }

        public byte[] EVEBytes { get; set; }

        public Dictionary<int, ushort> OFS { get; set; }
        public List<string> STR { get; set; }
    }
}
