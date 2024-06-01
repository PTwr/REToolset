using BinaryDataHelper;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVECommands;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVELines;
using System.Text;

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

        public string GetOrAddWeaponResource(string fallbackWP)
        {
            var sbytes = fallbackWP.ToBytes(Encoding.ASCII, fixedLength: 8);
            var prefetchLine = this.Blocks[1].EVELines[0];

            prefetchLine.Decompile();
            var existing = prefetchLine.ParsedCommands.OfType<ResourceLoad>()
                .Where(i => i.ResourceName.StartsWith("WP_"))
                .FirstOrDefault();

            if (existing is not null)
                return existing.ResourceName;

            EVEOpCode[] bytecode = [
                    new EVEOpCode(0x004BFFFF),
                    //string
                    new EVEOpCode(sbytes.Take(4)),
                    new EVEOpCode(sbytes.Skip(4).Take(4)),
                ];

            return fallbackWP;
        }

        public void AddPrefetchOfImgCutIn(string imgcutin)
        {
            var sbytes = imgcutin.ToBytes(Encoding.ASCII, fixedLength: 8);
            var prefetchLine = this.Blocks[1].EVELines[0];

            EVEOpCode[] bytecode = [
                    //cutin/avatar load
                    new EVEOpCode(prefetchLine, 0x00A4FFFF),
                    //string
                    new EVEOpCode(prefetchLine, sbytes.Take(4)),
                    new EVEOpCode(prefetchLine, sbytes.Skip(4).Take(4)),
                    //load cutin only
                    new EVEOpCode(prefetchLine, 0x00000001),
                ];

            prefetchLine.Decompile();
            var existing = prefetchLine.ParsedCommands.OfType<AvatarResourceLoad>()
                .Where(i => i.ResourceName == imgcutin)
                .FirstOrDefault();

            if (existing is null)
            {
                prefetchLine.Body.InsertRange(
                    prefetchLine.Body.Count - 1
                    , bytecode);
                prefetchLine.LineLengthOpCode.HighWord += 4;
            }
            else
            {
                prefetchLine.Body[existing.Pos + 0] = bytecode[0];
                prefetchLine.Body[existing.Pos + 1] = bytecode[1];
                prefetchLine.Body[existing.Pos + 2] = bytecode[2];
                prefetchLine.Body[existing.Pos + 3] = bytecode[3];
            }
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
