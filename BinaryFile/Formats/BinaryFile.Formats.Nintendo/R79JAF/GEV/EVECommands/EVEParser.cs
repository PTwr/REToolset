using BinaryDataHelper;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVELines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BinaryFile.Formats.Nintendo.R79JAF.GEV.EVECommands
{
    public static class EVEParser
    {
        public static IEnumerable<IEVECommand> Parse(IEnumerable<EVEOpCode> opcodes)
        {
            int parsedCount = 0;
            while (parsedCount < opcodes.Count())
            {
                int pc = 0;

                var slice = opcodes.Skip(parsedCount);
                var opcode = slice.First();

                //TODO include all opcodes hex in command.tostring
                if (opcode.HighWord == 0x0011)
                    yield return new Jump(slice, out pc);
                else if (opcode == 0x004BFFFF)
                    yield return new ResourceLoad(slice, out pc);
                else if (opcode == 0x0050FFFF)
                    yield return new ResourceLoadWithParam(slice, out pc);
                else if (opcode == 0x00A4FFFF)
                    yield return new AvatarResourceLoad(slice, out pc);
                else if (opcode == 0x00030000)
                    yield return new ScopeStart(slice, out pc);
                else if (opcode.HighWord == 0x006A)
                    yield return new PilotParamLoad(slice, out pc);
                else if (opcode.HighWord == 0x0056)
                    yield return new ObjLoad(slice, out pc);
                else if (opcode == 0x00000001 && slice.Count() >= 2)
                    yield return new ObjBind(slice, out pc);
                else if (opcode.HighWord == 0x00AE)
                    yield return new UnitSelectionAE(slice, out pc);
                else if (opcode.HighWord == 0x00AF)
                    yield return new UnitSelectionAF(slice, out pc);
                else
                    yield return new SingleOpCodeCommand(slice, out pc);

                parsedCount += pc;
            }
        }
    }

    public class PilotParamLoad : ResourceLoad
    {
        string str;
        ushort strId;
        string weaponName;
        public PilotParamLoad(IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(opCodes, out _)
        {
            strId = opCodes.First().LowWord;
            str = GetStr(strId);

            consumedOpCodes = 5;

            if (opCodes.ElementAt(3) == 0x82C882B5 && opCodes.ElementAt(4) == 0x00000000)
            {
                weaponName = "--none-- (0x82C882B5)";
            }
            else
            {
                weaponName = GetStr(3, 2, opCodes);
            }
            Hex(5, opCodes);
        }
        public override string ToString() => $"Pilot Param load: {resourceName} with {weaponName} for 0x{strId:X4} {str}{hex}";
    }
    public class ObjLoad : ResourceLoad
    {
        string str;
        ushort strId;
        string weaponName;
        public ObjLoad(IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(opCodes, out _)
        {
            strId = opCodes.First().LowWord;
            str = GetStr(strId);

            consumedOpCodes = 5;

            if (opCodes.ElementAt(3) == 0x82C882B5 && opCodes.ElementAt(4) == 0x00000000)
            {
                weaponName = "--none-- (0x82C882B5)";
            }
            else
            {
                weaponName = GetStr(3, 2, opCodes);
            }
            Hex(5, opCodes);
        }
        public override string ToString() => $"Obj load: {resourceName} with {weaponName} for 0x{strId:X4} {str}{hex}";
    }

    public class ObjBind : EVECommand
    {
        EVEOpCode body;
        public ObjBind(IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(opCodes)
        {
            consumedOpCodes = 2;
            body = opCodes.ElementAt(1);
            Hex(2, opCodes);
        }

        public override string ToString() => $"Obj Bind to 0x{body.LowWord:X4} {GetStr(body.LowWord)} with unknown value of 0x{body.HighWord:X4}{hex}";
    }

    public abstract class StringSelectionCommand : SingleOpCodeCommand
    {
        protected StringSelectionCommand(IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(opCodes, out consumedOpCodes)
        {
        }

        protected string S(string name) => $"For {name} 0x{body.LowWord:X4} {GetStr(body.LowWord)}";
    }

    public class UnitSelectionAE : StringSelectionCommand
    {
        EVEOpCode strRef;
        public UnitSelectionAE(IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(opCodes, out consumedOpCodes)
        {
            consumedOpCodes = 2;
            strRef = opCodes.ElementAt(1);
            Hex(2, opCodes);
        }

        public override string ToString() => $"{S("Unit AE")} place at 0x{strRef.LowWord:X4} {GetStr(strRef.LowWord)} with flag {strRef.HighWord:X4}{hex}";
    }

    public class UnitSelectionAF : StringSelectionCommand
    {
        EVEOpCode strRef;
        public UnitSelectionAF(IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(opCodes, out consumedOpCodes)
        {
            consumedOpCodes = 2;
            strRef = opCodes.ElementAt(1);
            Hex(2, opCodes);
        }

        public override string ToString() => $"{S("Unit AF")} move with 0x{strRef.LowWord:X4} {GetStr(strRef.LowWord)} with flag {strRef.HighWord:X4}{hex}";
    }

    public interface IEVECommand
    {
    }
    public abstract class EVECommand : IEVECommand
    {
        GEV gev;
        protected string hex;
        protected EVECommand(IEnumerable<EVEOpCode> opCodes)
        {
            gev = opCodes.First().ParentSegment.Parent;
        }

        protected string GetStr(int from, int count, IEnumerable<EVEOpCode> opcodes)
        {
            var str = opcodes.Skip(from).Take(count).SelectMany(i => i.ToBytes())
                .ToArray().AsSpan()
                .ToDecodedString(Encoding.ASCII);

            return str;
        }

        protected void Hex(int count, IEnumerable<EVEOpCode> opcodes)
        {
            var str = Environment.NewLine + string.Join(Environment.NewLine, opcodes.Take(count)
                .Select(i => "  " + i.ToString()));

            hex = str;
        }

        protected string GetStr(ushort id)
        {
            if (id < gev.STR.Count)
            {
                return gev.STR[id];
            }
            return $"INVALID STR ID {id:X4}";
        }
    }
    public class SingleOpCodeCommand : EVECommand
    {
        protected EVEOpCode body;
        public SingleOpCodeCommand(IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes)
            : base(opCodes)
        {
            consumedOpCodes = 1;
            body = opCodes.First();
        }

        public override string ToString() => body.ToString();
    }

    public class Jump : SingleOpCodeCommand
    {
        public Jump(IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(opCodes, out consumedOpCodes)
        {
            Hex(1, opCodes);
        }

        public int TargetLineId =>
            body.ParentLine.Parent.Parent.Blocks.First()
            .EVELines.OfType<EVEJumpTable>()
            .First().LineIdByJumpId(body.LowWord);

        public override string ToString() => $"Jump #{body.LowWord} Line #{TargetLineId} {hex}";
    }

    //Maybe some sort of scope nesting, seems to occur in conditionals/loop, and for some reason in resource load
    public class ScopeStart : SingleOpCodeCommand
    {
        public ScopeStart(IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(opCodes, out consumedOpCodes)
        {
            Hex(1, opCodes);
        }

        public override string ToString() => $"Scope(?){hex}";
    }

    public abstract class _ResourceLoad : EVECommand
    {
        protected string resourceName;
        public _ResourceLoad(IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes)
            : base(opCodes)
        {
            resourceName = GetStr(1, 2, opCodes);

            consumedOpCodes = 3;
            Hex(3, opCodes);
        }
    }

    public class ResourceLoad : _ResourceLoad
    {
        public ResourceLoad(IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(opCodes, out consumedOpCodes)
        {
        }
        public override string ToString() => $"Resource load 0x4B: {resourceName} {hex}";
    }
    public class ResourceLoadWithParam : _ResourceLoad
    {
        uint loadParam;
        public ResourceLoadWithParam(IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(opCodes, out _)
        {
            consumedOpCodes = 4;

            var param = opCodes.ElementAt(3);

            loadParam = param;
            Hex(4, opCodes);
        }
        public override string ToString() => "Resource load 0x50: " + resourceName + $" param: {loadParam:X8}{hex}";
    }
    public class AvatarResourceLoad : _ResourceLoad
    {
        bool loadImgCutIn;
        bool loadMSGBox;
        public AvatarResourceLoad(IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(opCodes, out _)
        {
            consumedOpCodes = 4;

            loadImgCutIn = opCodes.ElementAt(3).HighWord == 1;
            loadMSGBox = opCodes.ElementAt(3).LowWord == 1;
            Hex(4, opCodes);
        }
        public override string ToString() => $"Load avatar for{(loadImgCutIn ? " ImgCutIn" : "")} {(loadMSGBox ? " MsgBox" : "")}: {resourceName}{hex}";
    }


}
