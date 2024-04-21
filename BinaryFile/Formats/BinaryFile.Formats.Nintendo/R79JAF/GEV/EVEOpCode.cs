namespace BinaryFile.Formats.Nintendo.R79JAF.GEV
{
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
}
