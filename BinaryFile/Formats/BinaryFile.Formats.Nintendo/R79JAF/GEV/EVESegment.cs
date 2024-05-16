namespace BinaryFile.Formats.Nintendo.R79JAF.GEV
{
    public class EVESegment
    {
        public EVESegment(GEV parent)
        {
            Parent = parent;
        }

        //$EVE
        public string EVEMagic { get; set; }
        public List<EVEBlock> Blocks { get; set; }
        //0006FFFF
        public EVEOpCode Terminator { get; set; }
        public GEV Parent { get; }

        public virtual void Recompile()
        {
            int eveOffset = 0;
            foreach (var block in Blocks)
            {
                block.Recompile(eveOffset);

                eveOffset += block.OpCodeCount;
            }

            ushort lineId = 0;
            foreach(var line in Blocks.SelectMany(i=>i.EVELines))
            {
                line.LineStartOpCode.LowWord = lineId;
                lineId++;
            }
        }
        public virtual void Decompile()
        {
            foreach (var block in Blocks)
            {
                block.Decompile();
            }
        }
    }
}
