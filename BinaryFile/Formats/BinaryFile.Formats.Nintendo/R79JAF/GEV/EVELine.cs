using BinaryDataHelper;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVECommands;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.Marshaling;
using BinaryFile.MarshalingDI.Marshaling.Writing;
using System.Globalization;
using System.Text;

namespace BinaryFile.Formats.Nintendo.R79JAF.GEV
{
    public interface IEVELineBody
    {
        EVELine ParentLine { get; }
    }
    public abstract class _BaseEVELineBody : IEVELineBody
    {
        public EVELine ParentLine { get; }

        public _BaseEVELineBody(EVELine parent)
        {
            this.ParentLine = parent;
        }
    }
    public class EVELineRawBody : _BaseEVELineBody
    {
        public EVELineRawBody(EVELine parent)
            : base(parent) { }
        public virtual List<IEVEOpCode> OpCodes { get; set; } = new List<IEVEOpCode>();

        public override string ToString()
        {
            return string.Join(Environment.NewLine, OpCodes);
        }
    }
    public class EVEJumpTableLine : _BaseEVELineBody
    {
        //lineLength.LowWord = 0x0002

        public static readonly byte?[] Mask = [
            0x00, 0x03, 0x00, 0x00, // usual start of (non-conditional?) code line
            0x00, 0x14, null, null];// jumptable start | resource load method absolute opcode id?
        private readonly EVELine parent;

        public ushort InitMethodAbsoluteOpCodeId { get; set; }

        public List<(ushort jumpId, ushort targetOpCode)> RawJumpTable = new List<(ushort jumpId, ushort targetOpCode)>();

        public EVEJumpTableLine(EVELine parent) : base(parent)
        {
        }

        public override string ToString()
        {
            var eve = this.ParentLine.ParentBlock.ParentEVE;
            var linesByOffset = eve.Blocks.SelectMany(x => x.EVELines).ToDictionary(x => x.JumpOffset, x => x);

            var s = RawJumpTable
                .Select(x =>
                {
                    var line = linesByOffset[x.targetOpCode];
                    var blockId = eve.Blocks.IndexOf(line.ParentBlock);
                    return $"JumpId: 0x{x.jumpId:X4} | TargetOpCode: 0x{x.targetOpCode:X4} | LineId: #{line.LineId:D4} 0x{line.LineId:X4} | BlockId {blockId:D4} 0x{blockId:X4}";
                });

            var immediateExecLine = linesByOffset[InitMethodAbsoluteOpCodeId];
            var header = $"Jumptable of #{RawJumpTable.Count():D2} entries. Immediate exec line: #{immediateExecLine.LineId:D4} 0x{immediateExecLine.LineId:X4}";
            return header + Environment.NewLine + string.Join(Environment.NewLine, s);
        }
    }
    public class EVELine
    {
        public int ByteLength => LineOpCodeCount * 4;
        public EVELine(EVEBlock parent)
        {
            ParentBlock = parent;

            LineStartOpCode = new EVELineStartOpCode();
            LineLengthOpCode = new EVELineLengthOpCode();

            Terminator = new EVEOpCode(this, 0x0004, 0x0000);

        }
        public EVELine(EVEBlock parent, ushort lineLengthParam = 0x0002)
        {
            ParentBlock = parent;

            LineStartOpCode = new EVELineStartOpCode();
            LineLengthOpCode = new EVELineLengthOpCode()
            {
                LineLength = 3,
                UnknownLowWord = lineLengthParam,
            };

            Terminator = new EVEOpCode(this, 0x0004, 0x0000);
        }

        [Obsolete]
        public void AddEvcActorPrep(string objectName, string scnName, string pilotParam, int? pos = null)
        {
            var scnId = this.ParentBlock.ParentEVE.Parent_old.STR.IndexOf(scnName);
            if (scnId == -1)
            {
                this.ParentBlock.ParentEVE.Parent_old.STR.Add(scnName);
                scnId = this.ParentBlock.ParentEVE.Parent_old.STR.Count - 1;
            }

            var objBytes = objectName.ToBytes(BinaryStringHelper.Shift_JIS, fixedLength: 8);
            var noneBytes = "なし".ToBytes(BinaryStringHelper.Shift_JIS, fixedLength: 8);
            var ppBytes = pilotParam.ToBytes(BinaryStringHelper.Shift_JIS, fixedLength: 8);

            EVEOpCode[] bytecode = [
                new EVEOpCode(this, 0x0056, (ushort)scnId),

                new EVEOpCode(this, objBytes.Take(4)),
                new EVEOpCode(this, objBytes.Skip(4)),

                new EVEOpCode(this, noneBytes.Take(4)),
                new EVEOpCode(this, noneBytes.Skip(4)),

                new EVEOpCode(this, 0),

                new EVEOpCode(this, 0x006A, (ushort)scnId),

                new EVEOpCode(this, ppBytes.Take(4)),
                new EVEOpCode(this, ppBytes.Skip(4)),

                new EVEOpCode(this, noneBytes.Take(4)),
                new EVEOpCode(this, noneBytes.Skip(4)),

                new EVEOpCode(this, 0x00FA, (ushort)scnId),
                new EVEOpCode(this, (ushort)scnId, 0xFFFF),
                ];

            if (pos is null)
                Body.AddRange(bytecode);
            else
                Body.InsertRange(pos.Value, bytecode);
        }

        [Obsolete]
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

            LineLengthOpCode.LineLength = (ushort)(3 + Body.Count);

            //update Parsed form to match new body
            Decompile();
        }

        /// <summary>
        /// id of opode in EVE (index of 32bit chunks)
        /// </summary>
        public int JumpOffset { get; set; }

        public EVELineHeader LineHeader { get; set; }

        //Expected Instruction = 1
        [Obsolete]
        public EVELineStartOpCode LineStartOpCode { get; set; }
        public ushort LineId => LineHeader.LineId;

        //TODO analyze Param -> unknown, some kind of line type, or maybe nesting? Appears to be same in similar lines
        //Param = 0002 -> first line of block? but following lines can have either 03 or 05
        [Obsolete]
        public EVELineLengthOpCode LineLengthOpCode { get; set; }
        public int LineOpCodeCount => LineHeader.LineLength;
        public int BodyOpCodeCount => LineHeader.LineLength - 3; //without Start, Length, and Terminator, opcodes

        [Obsolete]
        public virtual List<EVEOpCode> Body { get; set; } = new List<EVEOpCode>();

        public IEVELineBody LineBody { get; set; }

        //00040000
        [Obsolete("Use WithMagicOf instead")]
        public EVEOpCode Terminator { get; set; }
        public EVEBlock ParentBlock { get; }

        [Obsolete]
        public virtual void Recompile(int eveOffset)
        {
            JumpOffset = eveOffset;
            LineLengthOpCode.LineLength = (ushort)(Body.Count + 3);
        }
        [Obsolete]
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
                LineHeader.ToString(),
                LineBody.ToString(),
            ]);
        }

        [Obsolete]
        public List<IEVECommand> ParsedCommands = new List<IEVECommand>();
    }
}
