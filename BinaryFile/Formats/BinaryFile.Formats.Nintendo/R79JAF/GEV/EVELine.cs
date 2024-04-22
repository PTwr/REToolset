namespace BinaryFile.Formats.Nintendo.R79JAF.GEV
{
    public class EVELine
    {
        public EVELine(EVEBlock parent)
        {
            Parent = parent;
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

        public virtual void Recompile()
        {
            LineLengthOpCode.Instruction = (ushort)(Body.Count + 3);
        }
        public virtual void Decompile()
        {
            //Derived classes will be doing fun stuff here
        }
    }
}
