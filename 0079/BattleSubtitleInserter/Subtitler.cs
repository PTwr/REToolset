﻿using BinaryDataHelper;
using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVECommands;
using ConcurrentCollections;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BattleSubtitleInserter
{
    public static class Subtitler
    {
        public static void EnsurePilotParamIsCreated(PilotParamHandler pph, EVCSceneHandler esc)
        {
            foreach (var voice in esc.VoiceFilesInUse())
            {
                EnsurePilotParamIsCreated(pph, voice);
            }
        }

        private static void EnsurePilotParamIsCreated(PilotParamHandler pph, string voice)
        {
            var ppcode = PilotParamHandler.VoiceFileToPilotPram(voice);

            pph.AddPilotParam(ppcode, voice);
        }

        public static void EnsureImgCutIsGenerated(EVCSceneHandler esc)
        {
            foreach (var voice in esc.VoiceFilesInUse())
            {
                EnsureImgCutIsGenerated(voice);
            }
        }

        public static void PrepareEvcActors(GEV gev, EVCSceneHandler esc, EVELine bodyLine, string subtitleModelName)
        {
            int subId = 0;
            foreach (var cut in esc.Cuts)
            {
                cut.RemoveFrameWaits();
                cut.RemoveImgCutIns();

                foreach (var voice in cut.Voices)
                {
                    //TODO check if voice is valid!
                    var ppcode = PilotParamHandler.VoiceFileToPilotPram(voice.VoiceName);

                    var scnName = $"SUB{subId:D2}";
                    var evcName = scnName;

                    Console.WriteLine($"Subtitling EVC. Voice: {voice.VoiceName} as {scnName}");

                    cut.AddUnit(scnName, evcName);
                    EnsureImgCutInIsPrefetched(gev, voice.VoiceName);
                    bodyLine.AddEvcActorPrep(subtitleModelName, scnName, ppcode);

                    //TODO (re)generate ImgCutIn image/brres

                    cut.AddImgCutIn(evcName, voice.Delay);

                    subId++;
                    //break;
                }
                cut.SaveNestedCut();
                //break;
            }
            esc.Save();
        }

        private static void EnsureImgCutInIsPrefetched(GEV gev, string voice)
        {
            gev.EVESegment.AddPrefetchOfImgCutIn(voice);
        }

        public static void ME09SpecialCase(PilotParamHandler pph, EVCSceneHandler esc, GEV gev, string subtitleModelName)
        {
            ushort rerouteFromLineId = 0x0037; //55
            int rerouteFromOpCodePos = 0;

            esc.ReplaceVoice("eva564", "sir017");
            EnsurePilotParamIsCreated(pph, esc);

            var line = gev.EVESegment.GetLineById(rerouteFromLineId); //#55
            EVELine bodyLine = gev.EVESegment.InsertRerouteBlock(line, rerouteFromOpCodePos, gev.EVESegment.GetLineById(0x0037), true);

            bodyLine?.Body.Add(new EVEOpCode(bodyLine, 0x0003, 0x0000));

            EnsurePilotParamIsCreated(pph, esc);
            EnsureImgCutIsGenerated(esc);

            EnsureVoicesGotPilotParam(pph, esc);

            PrepareEvcActors(gev, esc, bodyLine, subtitleModelName);
        }

        public static PilotParamHandler GetPPH()
        {
            var bootArc = MarshalingHelper.mU8.Deserialize(null, null, File.ReadAllBytes(Env.BootArcAbsolutePath()).AsMemory(), MarshalingHelper.ctx, out _);
            var pph = new PilotParamHandler(bootArc);
            return pph;
        }

        public static void SubtitleEVC(string gevPath, PilotParamHandler pph)
        {
            var gev = MarshalingHelper.mGEV.Deserialize(null, null, File.ReadAllBytes(gevPath).AsMemory(), MarshalingHelper.ctx, out _);
            var subtitleModelName = gev.EVESegment.GetOrAddWeaponResource("WP_EXA");

            var gevName = Path.GetFileNameWithoutExtension(gevPath).ToUpper();

            foreach(var line in gev.EVESegment.Blocks.SelectMany(i => i.EVELines).ToList())
            {
                if (line.LineId == 0x003C && gevName == "ME09")
                {
                    Console.WriteLine($"Special handling for ME09 EVC_ST_035");

                    string cutsceneName = "EVC_ST_035";
                    EVCSceneHandler esc = GetEscByName(cutsceneName);
                    ME09SpecialCase(pph, esc, gev, subtitleModelName);

                    Save(esc, Env.EVCFileAbsolutePath(cutsceneName));
                    continue;
                }

                DefaultCutsceneSubtitling(pph, gev, subtitleModelName, line);

                var facelessPlaybacks = line.ParsedCommands.OfType<FacelessVoicePlayback>();
                foreach (var facelessPlayback in facelessPlaybacks)
                {
                    Console.WriteLine($"Subtitling faceless voice playback in line #{line.LineId:D4} 0x{line.LineId:X4}");
                    Console.WriteLine($"Voice file: {facelessPlayback.Str}");

                    var index = facelessPlayback.Pos;
                    var avatarIndex = line.ParsedCommands.IndexOf(facelessPlayback) + 1;
                    if (avatarIndex < line.ParsedCommands.Count && line.ParsedCommands[avatarIndex] is AvatarDisplay avatar)
                    {
                        var sbytes = facelessPlayback.Str.ToBytes(Encoding.ASCII, fixedLength: 8);
                        var avatarPilotCode = avatar.Str.NullTrim();

                        //ingore minion pilot code
                        if (avatarPilotCode.StartsWith("sl", StringComparison.InvariantCultureIgnoreCase))
                            avatarPilotCode = null;

                        avatarPilotCode = SpecialCases.VoiceFileToAvatar.ContainsKey(facelessPlayback.Str) ? SpecialCases.VoiceFileToAvatar[facelessPlayback.Str] : avatarPilotCode;

                        EnsureImgCutIsGenerated(facelessPlayback.Str);
                        EnsurePilotParamIsCreated(pph, facelessPlayback.Str);
                        EnsureImgCutInIsPrefetched(gev, facelessPlayback.Str);

                        //insert line/block for avatar display
                        EVELine bodyLine = gev.EVESegment.InsertRerouteBlock(
                            gev.EVESegment.GetLineById(line.LineId),
                            facelessPlayback.Pos,
                            gev.EVESegment.GetLineById((ushort)(line.LineId + 1)),
                            true);

                        bodyLine.Body = [
                            //voice playback
                            new EVEOpCode(0x11B, facelessPlayback.OfsId),
                            new EVEOpCode(0x000A, 0xFFFF),

                            //avatar
                            new EVEOpCode(0x40A0, 0x0000),
                            new EVEOpCode(sbytes.Take(4)),
                            new EVEOpCode(sbytes.Skip(4).Take(4)),
                            new EVEOpCode(0x0002, 0xFFFF),
                            ];

                        //nullout rest of command
                        line.Body[facelessPlayback.Pos + 1] = new EVEOpCode(0);
                        line.Body[facelessPlayback.Pos + 2] = new EVEOpCode(0);
                        line.Body[facelessPlayback.Pos + 3] = new EVEOpCode(0);
                    }
                }

                var voicePlaybacks = line.ParsedCommands.OfType<VoicePlayback>();
                foreach (var  voicePlayback in voicePlaybacks)
                {
                    Console.WriteLine($"Subtitling generic voice playback in line #{line.LineId:D4} 0x{line.LineId:X4}");
                    Console.WriteLine($"Voice file: {voicePlayback.Str}");

                    var index = voicePlayback.Pos;
                    var avatarIndex = line.ParsedCommands.IndexOf(voicePlayback) + 1;
                    if (avatarIndex < line.ParsedCommands.Count && line.ParsedCommands[avatarIndex] is AvatarDisplay avatar)
                    {
                        var sbytes = voicePlayback.Str.ToBytes(Encoding.ASCII, fixedLength: 8);
                        var avatarPilotCode = avatar.Str.NullTrim();

                        //ingore minion pilot code
                        if (avatarPilotCode.StartsWith("sl", StringComparison.InvariantCultureIgnoreCase))
                            avatarPilotCode = null;

                        avatarPilotCode = SpecialCases.VoiceFileToAvatar.ContainsKey(voicePlayback.Str) ? SpecialCases.VoiceFileToAvatar[voicePlayback.Str] : avatarPilotCode;

                        EnsureImgCutIsGenerated(voicePlayback.Str);
                        EnsurePilotParamIsCreated(pph, voicePlayback.Str);
                        EnsureImgCutInIsPrefetched(gev, voicePlayback.Str);

                        //update avatar.Str to match voice.Str
                        line.Body[avatar.Pos + 1] = new EVEOpCode(line, sbytes.Take(4));
                        line.Body[avatar.Pos + 2] = new EVEOpCode(line, sbytes.Skip(4).Take(4));
                        //display ImgCutIn instead of MsgBox
                        line.Body[avatar.Pos + 3] = new EVEOpCode(line, 0x0002FFFF);
                    }
                }
            }

            Save(gev, gevPath);
        }

        private static void DefaultCutsceneSubtitling(PilotParamHandler pph, GEV? gev, string subtitleModelName, EVELine? line)
        {
            var evcPlaybacks = line.ParsedCommands.OfType<EVCPlayback>();

            foreach (var evcPlayback in evcPlaybacks)
            {
                Console.WriteLine($"Subtitling generic EVC playback in line #{line.LineId:D4} 0x{line.LineId:X4}");
                Console.WriteLine($"EVC file: {evcPlayback.Str}");

                var nextLine = gev.EVESegment.GetLineById((ushort)(line.LineId + 1));
                EVELine bodyLine = gev.EVESegment.InsertRerouteBlock(
                    gev.EVESegment.GetLineById(line.LineId),
                    evcPlayback.Pos,
                    nextLine, true);

                string cutsceneName = evcPlayback.Str;

                EVCSceneHandler esc = GetEscByName(cutsceneName);

                EnsurePilotParamIsCreated(pph, esc);
                EnsureImgCutIsGenerated(esc);

                EnsureVoicesGotPilotParam(pph, esc);

                PrepareEvcActors(gev, esc, bodyLine, subtitleModelName);

                //TODO better OpCode clone code :D
                bodyLine.Body.Add(new EVEOpCode(bodyLine, evcPlayback.OpCode.HighWord, evcPlayback.OpCode.LowWord));

                Save(esc, Env.EVCFileAbsolutePath(cutsceneName));
            }
        }

        private static void EnsureVoicesGotPilotParam(PilotParamHandler pph, EVCSceneHandler esc)
        {
            foreach (var voice in esc.VoiceFilesInUse())
            {
                var ppcode = PilotParamHandler.VoiceFileToPilotPram(voice);

                pph.AddPilotParam(ppcode, voice);
            }
        }

        private static EVCSceneHandler GetEscByName(string cutsceneName)
        {
            var evcST35Arc = MarshalingHelper.mU8.Deserialize(null, null, File.ReadAllBytes(Env.EVCFileAbsolutePath(cutsceneName)).AsMemory(), MarshalingHelper.ctx, out _);
            var esc = new EVCSceneHandler(evcST35Arc);
            return esc;
        }

        public static void Save(PilotParamHandler pph, string sourcePath)
        {
            pph.Save();

            var outputFile = Env.CleanToDirty(sourcePath);

            var bb = new ByteBuffer();
            MarshalingHelper.mU8.Serialize(pph.U8File, bb, MarshalingHelper.ctx, out _);
            File.WriteAllBytes(outputFile, bb.GetData());
        }
        public static void Save(EVCSceneHandler esc, string sourcePath)
        {
            esc.Save();

            var outputFile = Env.CleanToDirty(sourcePath);

            var bb = new ByteBuffer();
            MarshalingHelper.mU8.Serialize(esc.U8File, bb, MarshalingHelper.ctx, out _);
            File.WriteAllBytes(outputFile, bb.GetData());
        }
        public static void Save(GEV gev, string sourcePath)
        {
            var outputFile = Env.CleanToDirty(sourcePath);

            var bb = new ByteBuffer();
            MarshalingHelper.mGEV.Serialize(gev, bb, MarshalingHelper.ctx, out _);
            File.WriteAllBytes(outputFile, bb.GetData());

            R79JAFshared.GEVUnpacker.UnpackGev(MarshalingHelper.ctx, MarshalingHelper.mGEV, outputFile, 
                outputFile.Replace(".gev", "_mod").Replace("_clean", "_dirty"));

        }

        public static bool EnableImgCutInGeneration = true;
        public static ConcurrentHashSet<string> GeneratedImgCutIns = new ConcurrentHashSet<string>();
        public static void EnsureImgCutIsGenerated(string voiceFile)
        {
            var pilotCodeOverride = SpecialCases.VoiceFileToAvatar.ContainsKey(voiceFile) ? SpecialCases.VoiceFileToAvatar[voiceFile] : null;
            if (EnableImgCutInGeneration && GeneratedImgCutIns.Add(voiceFile))
                Env.PrepSubGen().RepackSubtitleTemplate(voiceFile, pilotCodeOverride);
        }
    }
}
