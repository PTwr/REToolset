using BinaryFile.Marshaling.Activation;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using BinaryDataHelper;

namespace BinaryFile.Formats.Nintendo.R79JAF.GEV.EVELines
{
    public class EVEJumpTable : EVELine
    {
        public override string ToString()
        {
            var linesByOffset = this.Parent.Parent.Blocks
                .SelectMany(i => i.EVELines)
                .ToDictionary(i => i.JumpOffset, i => i);

            var notes = this.jumps.Select(jump =>
            {
                var str = jump.ToString();
                var targetLine = linesByOffset[jump.JumpOffset];

                return $"{str} LineId=0x{targetLine.LineId:X4}";
            });

            return string.Join(Environment.NewLine, notes);
        }

        public EVEJumpTable(EVEBlock parent) : base(parent)
        {
        }

        public static readonly byte?[] Mask = [
            0x00, 0x01, null, null, // line start with whatever ID
            null, null, 0x00, 0x02, // whatever line length with unknown value of 0x02
            0x00, 0x03, 0x00, 0x00, // usual start of (non-conditional?) code line
            0x00, 0x14, null, null];// jumptable start with whatever jump count*2 (or rather, exit offset targeting Block Terminator?)

        List<EVEJumpTableEntry> jumps = new List<EVEJumpTableEntry>();
        public int LineIdByJumpId(ushort jumpId) => jumpId < jumps.Count ? this.jumps[jumpId].TargetedLine.LineId : -1;
        //TODO think about it a bit, putting Line "interpretation" as a field in Line could allow Decompile to be called upon Ctor
        public override void Decompile()
        {
        }

        public ushort AddJump(EVELine targetLine)
        {
            LineLengthOpCode.HighWord += 2;

            jumps.Add(new EVEJumpTableEntry(targetLine, (ushort)jumps.Count));
            return (ushort)(jumps.Count - 1);
        }
        public void RerouteJump(int jumpId, EVELine targetLine)
        {
            var jump = jumps[jumpId];
            jump.TargetedLine = targetLine;
        }

        private void DecodeJumpTable(List<EVEOpCode> body)
        {
            if (body[1].LowWord != LineOpCodeCount + 1)
                throw new Exception($"EVE Jumptable. Mismatch in jump count!");

            base.Decompile();
            jumps = new List<EVEJumpTableEntry>();
            for (int i = 2; i < body.Count; i += 2)
            {
                //With JumpTable being first Line there are no lines to link yet
                jumps.Add(new EVEJumpTableEntry(body[i], body[i + 1], null));
            }
        }

        public void LinkJumpsToLines()
        {
            var linesByOffset = this.Parent.Parent.Blocks
                .SelectMany(i => i.EVELines)
                .ToDictionary(i => i.JumpOffset, i => i);

            foreach(var jump in jumps)
            {
                var line = linesByOffset[jump.JumpOffset];
                jump.TargetedLine = line;
            }
        }

        public const ushort JumpTableHeaderOpCode = 0x0014;
        public const ushort JumpTableOffsetOpCode = 0x0013;
        public IEnumerable<EVEOpCode> EncodeJumpTable()
        {
            yield return new EVEOpCode(this, EVEOpCode.StatementStart);
            yield return new EVEOpCode(this, JumpTableHeaderOpCode, (ushort)(jumps.Count * 2 + 6));

            ushort jumpId = 0;
            foreach (var jump in jumps)
            {
                yield return new EVEOpCode(this, JumpTableOffsetOpCode, (ushort)jump.TargetedLine.JumpOffset);
                yield return new EVEOpCode(this, jump.JumpId, 0xFFFF);
                jumpId++;
            }
        }

        public override List<EVEOpCode> Body
        {
            get => EncodeJumpTable().ToList();
            set => DecodeJumpTable(value);
        }

        public override void Recompile(int eveOffset)
        {
            base.Recompile(eveOffset);
        }

        public static void Register(IMarshalerStore marshalerStore)
        {
            //TODO this has to be rewritten to more convienient form
            //TODO Derive(pattern) helper for automatic creation of CustomActivators
            //var baseMap = (marshalerStore.FindMarshaler<EVELine>() as RootTypeMarshaler<EVELine>);

            //TODO make recursive finder which won't hide basemap in wrapper class, or upgrade wrapper
            var baseMap = marshalerStore.FindRootMarshaler<EVELine>();
            var map = baseMap.Derive<EVEJumpTable>();

            //postprocessing requires all lines to be deserialized
            marshalerStore.FindRootMarshaler<EVESegment>()
                .AfterDeserialization((eve, l, ctx) =>
                {
                    var jumpTables = eve.Blocks
                        .SelectMany(i => i.EVELines)
                        .OfType<EVEJumpTable>();

                    foreach(var jumpTable in jumpTables)
                    {
                        jumpTable.LinkJumpsToLines();
                    }
                });

            //TODO think about inheriting it from base class? But usually derived classes will add fields, so it would be a niche usecase
            map.WithByteLengthOf(line => line.LineOpCodeCount * 4);

            //inject itself into base class activation
            //TODO some nice helper to reuse this logic
            baseMap.WithCustomActivator(new CustomActivator<EVEJumpTable, EVEBlock>((parent, data, ctx) =>
            {
                if (ctx.ItemSlice(data).Span.StartsWith(Mask.AsSpan()))
                    return new EVEJumpTable(parent);
                return null;
            }));

            //TODO map level Validator?
            map.AfterDeserialization((table, l, ctx) =>
            {
                //if (table.JumpCount != table.LineOpCodeCount - 5)
                //    throw new Exception($"EVE Jumptable. Mismatch in jump count!");
            });
        }
    }
    public class EVEJumpTableEntry
    {
        public EVEJumpTableEntry(EVEOpCode jump, EVEOpCode returnId, EVELine? targetedLine)
        {
            if (jump.HighWord != EVEJumpTable.JumpTableOffsetOpCode || returnId.LowWord != 0xFFFF)
                throw new Exception($"Malformed EVE Jumptable entry of {jump} {returnId}");

            JumpOffset = jump.LowWord;
            JumpId = returnId.HighWord;
            TargetedLine = targetedLine;
        }
        public EVEJumpTableEntry(EVELine? targetedLine, ushort jumpId)
        {
            TargetedLine = targetedLine;
            JumpId = jumpId;
        }

        public override string ToString()
        {
            return $"0x{JumpId:X2} -> DWORD: 0x{JumpOffset:X4} ABS:0x{JumpOffset*4+0x20:X8}";
        }

        //jump to 32bit aligned offset (opcode id), should be aligned with line starts
        public ushort JumpOffset { get; set; }
        //TODO validate, its possibly return label
        public ushort JumpId { get; set; }
        public EVELine TargetedLine { get; set; }
    }
}
