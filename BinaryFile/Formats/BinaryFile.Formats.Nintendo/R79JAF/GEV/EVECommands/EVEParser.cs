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
                    yield return new Jump(parsedCount, slice, out pc);
                else if (opcode == 0x004BFFFF)
                    yield return new ResourceLoad(parsedCount, slice, out pc);
                else if (opcode == 0x0050FFFF)
                    yield return new ResourceLoadWithParam(parsedCount, slice, out pc);
                else if (opcode == 0x00A4FFFF)
                    yield return new AvatarResourceLoad(parsedCount, slice, out pc);
                else if (opcode == 0x00030000)
                    yield return new ScopeStart(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x006A)
                    yield return new PilotParamLoad(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x0056)
                    yield return new ObjLoad(parsedCount, slice, out pc);
                else if (opcode == 0x00000001 && slice.Count() >= 2)
                    yield return new ObjBind(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x00AE)
                    yield return new UnitSelectionAE(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x00AF)
                    yield return new UnitSelectionAF(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x00FA)
                    yield return new EVCActorBind(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x00F9)
                    yield return new EVCPlayback(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x0059)
                    yield return new EVCActorUnbind(parsedCount, slice, out pc);
                //11B is event 11A is static?
                else if (opcode.HighWord == 0x011B || opcode.HighWord == 0x011A)
                    yield return new VoicePlayback(parsedCount, slice, out pc);
                else if (opcode == 0x40A00000 && slice.Count() >= 4)
                    yield return new AvatarDisplay(parsedCount, slice, out pc);
                else
                    yield return new SingleOpCodeCommand(parsedCount, slice, out pc);

                parsedCount += pc;
            }
        }
    }

    public class VoicePlayback : StringSelectionCommand
    {
        EVEOpCode flag;
        public VoicePlayback(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
        {
            consumedOpCodes = 2;
            flag = opCodes.ElementAt(1);
            Hex(2, opCodes);
        }

        public override string ToString()
        {
            return $"Voice playback 0x{body.LowWord:X4} {GetStr(body.LowWord)} with flag {flag}{hex}";
        }

        public string Str => GetStr(body.LowWord);
    }

    public class AvatarDisplay : EVECommand
    {
        string str;
        EVEOpCode flag;
        public AvatarDisplay(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes)
        {
            consumedOpCodes = 4;
            str = GetStr(1, 2, opCodes);
            flag = opCodes.ElementAt(3);
            Hex(4, opCodes);
        }

        public override string ToString()
        {
            var s = $"Avatar display for {str} ";

            if (flag == 0x0001FFFF)
                s += "as radio chatter";
            else if (flag == 0x0002FFFF)
                s += "as image cutin";
            else s += $"with flag {flag}";

            return s + hex;
        }

        public string Str => str;
    }

    public class PilotParamLoad : ResourceLoad
    {
        string str;
        ushort strId;
        string weaponName;
        public PilotParamLoad(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out _)
        {
            strId = opCodes.First().LowWord;
            str = GetStr(strId);

            consumedOpCodes = 5;

            if (opCodes.ElementAt(3) == 0x82C882B5 && opCodes.ElementAt(4) == 0x00000000)
            {
                weaponName = $"{GetStr(3, 2, opCodes)} (0x82C882B5 'none')";
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
        public ObjLoad(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out _)
        {
            strId = opCodes.First().LowWord;
            str = GetStr(strId);

            consumedOpCodes = 5;

            if (opCodes.ElementAt(3) == 0x82C882B5 && opCodes.ElementAt(4) == 0x00000000)
            {
                weaponName = $"{GetStr(3, 2, opCodes)} (0x82C882B5 'none')";
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
        public ObjBind(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes)
        {
            consumedOpCodes = 2;
            body = opCodes.ElementAt(1);
            Hex(2, opCodes);
        }

        public override string ToString() => $"Obj Bind to 0x{body.LowWord:X4} {GetStr(body.LowWord)} with unknown value of 0x{body.HighWord:X4}{hex}";
    }

    public abstract class StringSelectionCommand : SingleOpCodeCommand
    {
        protected StringSelectionCommand(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
        {
        }

        protected string S(string name) => $"For {name} 0x{body.LowWord:X4} {GetStr(body.LowWord)}";
    }

    public class EVCActorBind : EVECommand
    {
        ushort pilotStrId;
        EVEOpCode body;
        public EVCActorBind(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes)
        {
            pilotStrId = opCodes.First().LowWord;
            consumedOpCodes = 2;
            body = opCodes.ElementAt(1);
            Hex(2, opCodes);
        }

        public override string ToString() => $"EVC Actor Bind 0x{pilotStrId:X4} {GetStr(pilotStrId)} to 0x{body.HighWord:X4} {GetStr(body.HighWord)} with unknown value of 0x{body.LowWord:X4}{hex}";
    }
    public class EVCActorUnbind : EVECommand
    {
        ushort pilotStrId;
        EVEOpCode body;
        public EVCActorUnbind(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes)
        {
            pilotStrId = opCodes.First().LowWord;
            consumedOpCodes = 2;
            body = opCodes.ElementAt(1);
            Hex(2, opCodes);
        }

        public override string ToString() => $"EVC Actor Unbind(?) 0x{pilotStrId:X4} {GetStr(pilotStrId)} to 0x{body.HighWord:X4} {GetStr(body.HighWord)} with unknown value of 0x{body.LowWord:X4}{hex}";
    }
    public class EVCPlayback : StringSelectionCommand
    {
        public EVCPlayback(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
        {
            Hex(1, opCodes);
        }

        public override string ToString()
        {
            return $"EVC Playback 0x{body.LowWord:X4} {GetStr(body.LowWord)}{hex}";
        }

        public string Str => GetStr(body.LowWord);
    }


    public class UnitSelectionAE : StringSelectionCommand
    {
        EVEOpCode strRef;
        public UnitSelectionAE(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
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
        public UnitSelectionAF(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
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

        public int Pos { get; }

        protected EVECommand(int pos, IEnumerable<EVEOpCode> opCodes)
        {
            gev = opCodes.First().ParentSegment.Parent;
            Pos = pos;
        }

        protected string GetStr(int from, int count, IEnumerable<EVEOpCode> opcodes)
        {
            var str = opcodes.Skip(from).Take(count).SelectMany(i => i.ToBytes())
                .ToArray().AsSpan()
                .ToDecodedString(BinaryStringHelper.Shift_JIS);

            return str.NullTrim();
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
        public SingleOpCodeCommand(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes)
            : base(pos, opCodes)
        {
            consumedOpCodes = 1;
            body = opCodes.First();
        }

        public override string ToString() => body.ToString();
    }

    public class Jump : SingleOpCodeCommand
    {
        public Jump(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
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
        public ScopeStart(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
        {
            Hex(1, opCodes);
        }

        public override string ToString() => $"Scope(?){hex}";
    }

    public abstract class _ResourceLoad : EVECommand
    {
        protected string resourceName;
        public _ResourceLoad(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes)
            : base(pos, opCodes)
        {
            resourceName = GetStr(1, 2, opCodes);

            consumedOpCodes = 3;
            Hex(3, opCodes);
        }
    }

    public class ResourceLoad : _ResourceLoad
    {
        public ResourceLoad(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
        {
        }
        public override string ToString() => $"Resource load 0x4B: {resourceName} {hex}";
    }
    public class ResourceLoadWithParam : _ResourceLoad
    {
        uint loadParam;
        public ResourceLoadWithParam(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out _)
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
        public AvatarResourceLoad(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out _)
        {
            consumedOpCodes = 4;

            loadImgCutIn = opCodes.ElementAt(3).HighWord == 1;
            loadMSGBox = opCodes.ElementAt(3).LowWord == 1;
            Hex(4, opCodes);
        }
        public override string ToString() => $"Load avatar for{(loadImgCutIn ? " ImgCutIn" : "")} {(loadMSGBox ? " MsgBox" : "")}: {resourceName}{hex}";
    }


}
