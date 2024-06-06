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
                else if (opcode.HighWord == 0x00AE)
                    yield return new UnitSelectionAE(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x00AF)
                    yield return new UnitSelectionAF(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x00FA)
                    yield return new EVCActorBind(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x0100 && opcode.LowWord == 0xFFFF && slice.Count() >= 4)
                    yield return new UnknownEVCPreparation(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x00F9 || opcode.HighWord == 0x00FD || opcode.HighWord == 0x00FF)
                    yield return new EVCPlayback(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x0059)
                    yield return new SetObjectPosition(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x0063 || opcode.HighWord == 0x0058)
                    yield return new DoSomethingWithObject(parsedCount, slice, out pc);
                //11B is event 11A is static?
                else if (opcode.HighWord == 0x011B || opcode.HighWord == 0x011A)
                    yield return new VoicePlayback(parsedCount, slice, out pc);
                //used in Tutorial and once in ME05 start
                else if (opcode.HighWord == 0x0115)
                    yield return new FacelessVoicePlayback(parsedCount, slice, out pc);
                else if (opcode == 0x40A00000 && slice.Count() >= 4)
                    yield return new AvatarDisplay(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x0009)
                    yield return new RelativeJump(parsedCount, slice, out pc);
                else if (opcode == 0x00CBFFFF)
                    yield return new MissionSuccess(parsedCount, slice, out pc);
                else if (opcode == 0x00CCFFFF)
                    yield return new MissionFailure(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x0046)
                    yield return new EventSubscription(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x0054)
                    yield return new Unknown(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x0067)
                    yield return new Unknown(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x0066)
                    yield return new Unknown(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x00CE)
                    yield return new Unknown(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x00C1)
                    yield return new TextBox(parsedCount, slice, out pc);
                else if (opcode.HighWord == 0x0057)
                    yield return new Despawn(parsedCount, slice, out pc);
                else
                    yield return new SingleOpCodeCommand(parsedCount, slice, out pc);

                parsedCount += pc;
            }
        }
    }

    public class TextBox : EVECommand
    {
        public EVEOpCode StrRef;
        public EVEOpCode Unknown;
        public TextBox(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes)
        {
            consumedOpCodes = 2;
            Hex(2, opCodes);

            StrRef = opCodes.ElementAt(0);
            Unknown = opCodes.ElementAt(1);
        }

        public string Str => GetStr(StrRef.LowWord);
        public override string ToString() => $"#TextBox #{StrRef.LowWord:D4} 0x{StrRef.LowWord:X4} with unknown of {Unknown} {Environment.NewLine}{Str} {hex}";
    }

    public class Despawn : SingleOpCodeCommand
    {
        public Despawn(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
        {
        }

        public override string ToString() => $"#Despawn '{GetStr(OpCode.LowWord)}' #{OpCode.LowWord:D4} 0x{OpCode.LowWord:X4} {hex}";
    }

    public class Unknown : SingleOpCodeCommand
    {
        public Unknown(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
        {
        }

        public override string ToString() => $"#Unknown 0x{OpCode.HighWord:X4} str ref '{GetStr(OpCode.LowWord)}' #{OpCode.LowWord:D4} 0x{OpCode.LowWord:X4} {hex}";
    }
    public class EventSubscription : EVECommand
    {
        public EVEOpCode Header;
        public EVEOpCode Param;
        public EventSubscription(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes)
        {
            consumedOpCodes = 1;
            Hex(1, opCodes);

            Header = opCodes.ElementAt(0);
            //Param = opCodes.ElementAt(1);
        }

        public override string ToString() => $"#Event Subscription #{Header.LowWord:D4} 0x{Header.LowWord:X4} {hex}";
    }

    public class MissionFailure : EVECommand
    {
        public MissionFailure(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes)
        {
            consumedOpCodes = 1;
            Hex(1, opCodes);
        }

        public override string ToString() => $"#Mission Failed{hex}";
    }
    public class MissionSuccess : EVECommand
    {
        public MissionSuccess(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes)
        {
            consumedOpCodes = 1;
            Hex(1, opCodes);
        }

        public override string ToString() => $"#Mission Success{hex}";
    }

    public class RelativeJump : EVECommand
    {
        public short RelativeJumpOffset { get; }
        public int AbsoluteJumpOffset { get; }
        public EVELine? TargetLine => this.gev.EVESegment.Blocks.SelectMany(i => i.EVELines)
                .Where(i => i.JumpOffset == AbsoluteJumpOffset)
                .FirstOrDefault();
        public EVELine? ClosestMatch => this.gev.EVESegment.Blocks.SelectMany(i => i.EVELines)
                .Select(line => new { line, diff = line.JumpOffset - AbsoluteJumpOffset })
                .OrderBy(i => Math.Abs(i.diff))
                .FirstOrDefault()?.line;

        public EVEOpCode Body { get; }
        public RelativeJump(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes)
        {
            consumedOpCodes = 1;

            RelativeJumpOffset = (short)opCodes.First().LowWord;
            AbsoluteJumpOffset = opCodes.First().ParentLine.JumpOffset - RelativeJumpOffset;

            AbsoluteJumpOffset = opCodes.First().ParentLine.JumpOffset + RelativeJumpOffset;

            Hex(1, opCodes);

            Body = opCodes.First();
        }

        public override string ToString()
        {
            return $"#Relative Jump of {RelativeJumpOffset} 0x{RelativeJumpOffset:X4} (Abs {AbsoluteJumpOffset} 0x{AbsoluteJumpOffset:X4})"
                + Environment.NewLine +
                $"Probably to Line #{TargetLine?.LineId:D4} 0x{TargetLine?.LineId:X4}"
                + Environment.NewLine + 
                $"Closest match: {ClosestMatch?.LineId:D4} 0x{ClosestMatch?.LineId:X4} diff by {ClosestMatch?.JumpOffset - AbsoluteJumpOffset}"+
                $"{hex}";
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
            return $"#Voice playback 0x{OpCode.LowWord:X4} {GetStr(OpCode.LowWord)} with flag {flag}{hex}";
        }

        public string Str => GetStr(OpCode.LowWord);
    }
    public class FacelessVoicePlayback : StringSelectionCommand
    {
        public EVEOpCode flag;
        public FacelessVoicePlayback(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
        {
            consumedOpCodes = 2;
            flag = opCodes.ElementAt(1);
            Hex(2, opCodes);
        }

        public override string ToString()
        {
            return $"#Faceless Voice playback 0x{OpCode.LowWord:X4} {GetStr(OpCode.LowWord)} with flag {flag}{hex}";
        }

        public string Str => GetStr(OpCode.LowWord);
        public ushort OfsId => OpCode.LowWord;
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
            var s = $"#Avatar display for {str} ";

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
        public override string ToString() => $"#Pilot Param load: {ResourceName} with {weaponName} for 0x{strId:X4} {str}{hex}";
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
        public override string ToString() => $"#Obj load: {ResourceName} with {weaponName} for 0x{strId:X4} {str}{hex}";
    }

    public abstract class StringSelectionCommand : SingleOpCodeCommand
    {
        protected StringSelectionCommand(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
        {
        }

        protected string S(string name) => $"#For {name} 0x{OpCode.LowWord:X4} {GetStr(OpCode.LowWord)}";
    }

    public class UnknownEVCPreparation : EVECommand
    {
        string resourceName;
        EVEOpCode flag;
        public UnknownEVCPreparation(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes)
        {
            resourceName = GetStr(1, 2, opCodes);
            consumedOpCodes = 4;

            flag = opCodes.ElementAt(3);
            Hex(4, opCodes);
        }

        public override string ToString() => $"#EVC Preparation for {resourceName} with unknown value of {flag}{hex}";
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

        public override string ToString() => $"#EVC Actor Bind 0x{pilotStrId:X4} {GetStr(pilotStrId)} to 0x{body.HighWord:X4} {GetStr(body.HighWord)} with unknown value of 0x{body.LowWord:X4}{hex}";
    }
    public class SetObjectPosition : EVECommand
    {
        ushort pilotStrId;
        EVEOpCode body;
        public SetObjectPosition(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes)
        {
            pilotStrId = opCodes.First().LowWord;
            consumedOpCodes = 2;
            body = opCodes.ElementAt(1);
            Hex(2, opCodes);
        }

        public override string ToString() => $"#Set Object Position of 0x{pilotStrId:X4} {GetStr(pilotStrId)} to 0x{body.HighWord:X4} {GetStr(body.HighWord)} with unknown value of 0x{body.LowWord:X4}{hex}";
    }
    public class DoSomethingWithObject : EVECommand
    {
        ushort pilotStrId;
        EVEOpCode body;
        public DoSomethingWithObject(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes)
        {
            pilotStrId = opCodes.First().LowWord;
            consumedOpCodes = 2;
            body = opCodes.ElementAt(1);
            Hex(2, opCodes);
        }

        public override string ToString() => $"#Do something with object 0x{pilotStrId:X4} {GetStr(pilotStrId)} with unknown param of {body}{hex}";
    }
    public class EVCPlayback : StringSelectionCommand
    {
        public EVCPlayback(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
        {
            Hex(1, opCodes);
        }

        public override string ToString()
        {
            return $"#EVC Playback 0x{OpCode.LowWord:X4} {GetStr(OpCode.LowWord)}{hex}";
        }

        public string Str => GetStr(OpCode.LowWord);
        public ushort OfsId => OpCode.LowWord;
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

        public override string ToString() => $"#{S("Unit AE")} place at 0x{strRef.LowWord:X4} {GetStr(strRef.LowWord)} with flag {strRef.HighWord:X4}{hex}";
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

        public override string ToString() => $"#{S("Unit AF")} move with 0x{strRef.LowWord:X4} {GetStr(strRef.LowWord)} with flag {strRef.HighWord:X4}{hex}";
    }

    public interface IEVECommand
    {
    }
    public abstract class EVECommand : IEVECommand
    {
        protected GEV gev;
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
        public EVEOpCode OpCode { get; }
        public SingleOpCodeCommand(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes)
            : base(pos, opCodes)
        {
            consumedOpCodes = 1;
            OpCode = opCodes.First();
            Hex(1, opCodes);
        }

        public override string ToString() => OpCode.ToString();
    }

    public class Jump : SingleOpCodeCommand
    {
        public Jump(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
        {
            Hex(1, opCodes);
        }

        public ushort JumpId => OpCode.LowWord;
        public int TargetLineId =>
            OpCode.ParentLine.Parent.Parent.Blocks.First()
            .EVELines.OfType<EVEJumpTable>()
            .First().LineIdByJumpId(OpCode.LowWord);

        public override string ToString() => $"#Jump #{OpCode.LowWord} Line 0x{TargetLineId:X4} #{TargetLineId:D4} {hex}";
    }

    //Maybe some sort of scope nesting, seems to occur in conditionals/loop, and for some reason in resource load
    public class ScopeStart : SingleOpCodeCommand
    {
        public ScopeStart(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
        {
            Hex(1, opCodes);
        }

        public override string ToString() => $"#Scope(?){hex}";
    }

    public abstract class _ResourceLoad : EVECommand
    {
        public string ResourceName { get; }
        public _ResourceLoad(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes)
            : base(pos, opCodes)
        {
            ResourceName = GetStr(1, 2, opCodes);

            consumedOpCodes = 3;
            Hex(3, opCodes);
        }
    }

    public class ResourceLoad : _ResourceLoad
    {
        public ResourceLoad(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out consumedOpCodes)
        {
        }
        public override string ToString() => $"#Resource load 0x4B: {ResourceName} {hex}";
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
        public override string ToString() => "#Resource load 0x50: " + ResourceName + $" param: {loadParam:X8}{hex}";
    }
    public class AvatarResourceLoad : _ResourceLoad
    {
        bool loadImgCutIn;
        bool loadMSGBox;
        public AvatarResourceLoad(int pos, IEnumerable<EVEOpCode> opCodes, out int consumedOpCodes) : base(pos, opCodes, out _)
        {
            consumedOpCodes = 4;

            loadMSGBox = opCodes.ElementAt(3).HighWord == 1;
            loadImgCutIn = opCodes.ElementAt(3).LowWord == 1;
            Hex(4, opCodes);
        }
        public override string ToString() => $"#Load avatar for{(loadImgCutIn ? " ImgCutIn" : "")} {(loadMSGBox ? " MsgBox" : "")}: {ResourceName}{hex}";
    }


}
