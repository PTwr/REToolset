namespace BinaryFile.Formats.Nintendo.R79JAF.GEV
{
    //TODO implicit converters to compare with 32bit hex mask?
    //Record provides struct-like default equality semantics
    public record class EVEOpCode
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
        public EVEOpCode(EVELine parent, ushort instruction, ushort parameter)
        {
            ParentLine = parent;
            Instruction = instruction;
            Parameter = parameter;
        }
        public EVEOpCode(ushort instruction, ushort parameter)
        {
            Instruction = instruction;
            Parameter = parameter;
        }
        public EVEOpCode(EVELine parent, uint code)
        {
            ParentLine = parent;
            Instruction = (ushort)(code >> 16);
            Parameter = (ushort)(code);
        }
        public EVEOpCode(uint code)
        {
            Instruction = (ushort)(code >> 16);
            Parameter = (ushort)(code);
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

        public static implicit operator uint(EVEOpCode code)
        {
            return (uint)(code.Instruction << 16 | code.Parameter);
        }

        //TODO check if lower word matters, might be just null/one padding 
        public const uint StatementStart = 0x00030000;
        public const uint LineTerminator = 0x00040000;
        public const uint BlockTerminator = 0x0005FFFF;
        public const uint SegmentTerminator = 0x0006FFFF;
    }
}
