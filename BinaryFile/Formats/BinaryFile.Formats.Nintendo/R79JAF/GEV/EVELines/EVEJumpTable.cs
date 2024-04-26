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
        //TODO think about it a bit, putting Line "interpretation" as a field in Line could allow Decompile to be called upon Ctor
        public override void Decompile()
        {
            if (Body[1].Parameter != LineOpCodeCount + 1)
                throw new Exception($"EVE Jumptable. Mismatch in jump count!");

            base.Decompile();
            jumps = new List<EVEJumpTableEntry>();
            for (int i = 2; i < Body.Count; i += 2)
            {
                jumps.Add(new EVEJumpTableEntry(Body[i], Body[i + 1]));
            }
        }

        public override void Recompile()
        {
            Body.Clear();

            Body.Add(new EVEOpCode(this, 0x0003, 0x0000));
            Body.Add(new EVEOpCode(this, 0x0014, (ushort)(jumps.Count*2 + 6)));

            //TODO recalculate Line.JumpOffsets, then update jump.JumpOffset accordingly
            //EVESegment should perform BeforeSerialize mass update of Line.Id, and could pass old+new line offset to recreate jump table at tha time
            //it is requierd for adding extra lines and opcodes

            ushort jumpId = 0;
            foreach (var jump in jumps)
            {
                Body.Add(new EVEOpCode(this, 0x0013, (ushort)jump.JumpOffset));
                Body.Add(new EVEOpCode(this, jumpId, 0xFFFF));
                jumpId++;
            }

            base.Recompile();
        }

        public static void Register(IMarshalerStore marshalerStore)
        {
            //TODO this has to be rewritten to more convienient form
            //TODO Derive(pattern) helper for automatic creation of CustomActivators
            //var baseMap = (marshalerStore.FindMarshaler<EVELine>() as RootTypeMarshaler<EVELine>);

            //TODO make recursive finder which won't hide basemap in wrapper class, or upgrade wrapper
            var baseMap = marshalerStore.FindRootMarshaler<EVELine>();
            var map = baseMap.Derive<EVEJumpTable>();

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
        public EVEJumpTableEntry(EVEOpCode jump, EVEOpCode returnId)
        {
            if (jump.Instruction != 0x00013 || returnId.Parameter != 0xFFFF)
                throw new Exception($"Malformed EVE Jumptable entry of {jump} {returnId}");

            JumpOffset = jump.Parameter;
            JumpId = returnId.Instruction;
        }

        public override string ToString()
        {
            return $"0x{JumpId:X2} -> 0x{JumpOffset:X4}";
        }

        //jump to 32bit aligned offset (opcode id), should be aligned with line starts
        public int JumpOffset { get; set; }
        //TODO validate, its possibly return label
        public int JumpId { get; set; }
    }
}
