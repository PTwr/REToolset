namespace BinaryFile.Formats.Nintendo.R79JAF.GEV
{
    public class EVEBlock
    {
        public EVEBlock(EVESegment parent)
        {
            Parent = parent;
        }

        public List<EVELine> EVELines { get; set; }
        //0005FFFF
        public EVEOpCode Terminator { get; set; }
        public EVESegment Parent { get; }

        public virtual int ByteLength => OpCodeCount * 4;
        public int OpCodeCount => EVELines.Sum(i => i.LineOpCodeCount) + 1;

        public virtual void Recompile(int eveOffset)
        {
            foreach (var line in EVELines)
            {
                line.Recompile(eveOffset);
                eveOffset += line.LineOpCodeCount;
            }
        }
        public virtual void Decompile()
        {
            foreach (var line in EVELines)
            {
                line.Decompile();
            }
        }
    }
}
