namespace BinaryFile.Formats.Nintendo.R79JAF.GEV
{
    public class EVEBlock
    {
        public EVEBlock(EVESegment parent)
        {
            Parent = parent;
        }

        //no way to tell EVELine count before parsing? Gotta lurk for Terminator?
        public List<EVELine> EVELines { get; set; }
        public virtual int ByteLength => (EVELines.Sum(l => l.LineOpCodeCount) + 1) * 4;
        //0005FFFF
        public EVEOpCode Terminator { get; set; }
        public EVESegment Parent { get; }
    }
}
