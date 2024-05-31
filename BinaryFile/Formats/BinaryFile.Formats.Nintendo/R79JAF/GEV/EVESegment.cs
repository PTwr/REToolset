using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVECommands;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVELines;

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
            foreach (var line in Blocks.SelectMany(i => i.EVELines))
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

        public EVELine GetLineById(ushort lineId)
        {
            return this.Blocks
                .SelectMany(i => i.EVELines)
                .Where(i => i.LineId == lineId)
                .FirstOrDefault();
        }

        /// <summary>
        /// Inserts EVEBlock with Body Line and Jump Line
        /// </summary>
        /// <param name="originLine">Line where new Jump will be inserted in</param>
        /// <param name="bodyOpCodePos">OpCode position for new Jump</param>
        /// <param name="returnLine">Line to which execution will be returned</param>
        /// <param name="overwriteJumpOpCode">Whether or not Jump OpCode will overwrite opcode at given position</param>
        /// <returns>Body Line of inserted block</returns>
        public EVELine InsertRerouteBlock(EVELine originLine, int bodyOpCodePos, EVELine returnLine, bool overwriteJumpOpCode = true)
        {
            var block = new EVEBlock(this);
            this.Blocks.Add(block);

            var bodyLine = new EVELine(block);
            var jumpLine = new EVELine(block);
            block.EVELines = [bodyLine, jumpLine];

            //JumpTable will recalculate Offsets automagically :)
            var jumpId = JumpTable.AddJump(bodyLine);
            var returnJumpId = JumpTable.AddJump(returnLine);

            //add jumpout out of original logic to new block
            var jumpOpCode = new EVEOpCode(originLine, 0x0011, jumpId);
            if (overwriteJumpOpCode)
            {
                originLine.Body[bodyOpCodePos] = jumpOpCode;
            }
            else
            {
                originLine.Body.Insert(bodyOpCodePos, jumpOpCode);
            }

            //just a line with return jump to original logic
            jumpLine.Body = [
                new EVEOpCode(jumpLine, 0x0011, returnJumpId)
                ];
            jumpLine.LineLengthOpCode.HighWord = 4;

            return bodyLine;
        }

        public EVEJumpTable JumpTable => this.Blocks[0].EVELines[0] as EVEJumpTable;
    }
}
