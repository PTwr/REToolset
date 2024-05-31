using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVECommands;
using System.Globalization;

namespace BinaryFile.Formats.Nintendo.R79JAF.GEV
{
    public class EVELine
    {
        public EVELine(EVEBlock parent)
        {
            Parent = parent;

            LineStartOpCode = new EVEOpCode(this, 0x0001, 0x0000);
            LineLengthOpCode = new EVEOpCode(this, 3, 0x0002);

            Terminator = new EVEOpCode(this, 0x0004, 0x0000);

        }
        public EVELine(EVEBlock parent, ushort lineLengthParam = 0x0002)
        {
            Parent = parent;

            LineStartOpCode = new EVEOpCode(this, 0x0001, 0x0000);
            LineLengthOpCode = new EVEOpCode(this, 3, lineLengthParam);

            Terminator = new EVEOpCode(this, 0x0004, 0x0000);
        }

        public void SetBody(string textform)
        {
            var lines = textform
                .Split(new char[] { '\r', '\n' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(i => !i.StartsWith('#'));

            var ushortPairs = lines
                .Select(i => i.Split(new char[] { ' ' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray())
                .Where(i => i.Length is 2)
                .Where(i => i[0].Length is 4)
                .Where(i => i[1].Length is 4)
                .Select(i => new EVEOpCode(this, ushort.Parse(i[0], NumberStyles.HexNumber), ushort.Parse(i[1], NumberStyles.HexNumber)));

            this.Body = ushortPairs.ToList();

            this.LineLengthOpCode.HighWord = (ushort)(3 + Body.Count);

            //update Parsed form to match new body
            Decompile();
        }

        /// <summary>
        /// id of opode in EVE (index of 32bit chunks)
        /// </summary>
        public int JumpOffset { get; set; }

        //Expected Instruction = 1
        public EVEOpCode LineStartOpCode { get; set; }
        public ushort LineId => LineStartOpCode.LowWord;

        //TODO analyze Param -> unknown, some kind of line type, or maybe nesting? Appears to be same in similar lines
        //Param = 0002 -> first line of block? but following lines can have either 03 or 05
        public EVEOpCode LineLengthOpCode { get; set; }
        public int LineOpCodeCount => LineLengthOpCode.HighWord;
        public int BodyOpCodeCount => LineLengthOpCode.HighWord - 3; //without Start, Length, and Terminator, opcodes

        public virtual List<EVEOpCode> Body { get; set; } = new List<EVEOpCode>();

        //00040000
        public EVEOpCode Terminator { get; set; }
        public EVEBlock Parent { get; }

        public virtual void Recompile(int eveOffset)
        {
            JumpOffset = eveOffset;
            LineLengthOpCode.HighWord = (ushort)(Body.Count + 3);
        }
        public virtual void Decompile()
        {
            //Derived classes will be doing fun stuff here

            ParsedCommands = EVEParser.Parse(Body).ToList();
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, [
                $"Jump Offset: 0x{JumpOffset:X4}",
                $"GEV byte offset: 0x{(JumpOffset * 4 + 0x20):X4}",
                "Line Start:" + LineStartOpCode.ToString(),
                "Line Length: " + LineLengthOpCode.ToString(),
                ..ParsedCommands.Select(i => i.ToString()),
                $"Line Terminator: {Terminator.ToString()}"
            ]);
        }

        public List<IEVECommand> ParsedCommands = new List<IEVECommand>();
    }
}
