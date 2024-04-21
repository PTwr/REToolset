using BinaryFile.Unpacker.Marshalers;

namespace BinaryFile.Formats.Nintendo.R79JAF.GEV
{
    //TODO move raw OpCodes to RawEVEBlock? Or make base interface?
    //TODO JumpTable does not need raw OpCodes but header/footer and validation could be reused
    //TODO EVELine should have Compile() method which would refresh bytecode from metadata, Base EVELine would prepare header and footer, derived classes would fill in body
    [Obsolete("Switch to inheritance from EVELine")]
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
}
