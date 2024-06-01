using BinaryDataHelper;
using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVECommands;
using ConcurrentCollections;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleSubtitleInserter
{
    public static class Subtitler
    {
        public static void EnsureImgCutInIsLoaded(PilotParamHandler pph, EVCSceneHandler esc)
        {
            foreach (var voice in esc.VoiceFilesInUse())
            {
                var ppcode = PilotParamHandler.VoiceFileToPilotPram(voice);

                pph.AddPilotParam(ppcode, voice);
            }
        }
        public static void EnsureImgCutIsGenerated(EVCSceneHandler esc)
        {
            foreach (var voice in esc.VoiceFilesInUse())
            {
                CreateImgCutIn(voice);
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

                    cut.AddUnit(scnName, evcName);
                    gev.EVESegment.AddPrefetchOfImgCutIn(voice.VoiceName);
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

        public static void ME09SpecialCase(PilotParamHandler pph, EVCSceneHandler esc, GEV gev, string subtitleModelName)
        {
            ushort rerouteFromLineId = 0x0037; //55
            int rerouteFromOpCodePos = 0;

            esc.ReplaceVoice("eva564", "sir017");
            EnsureImgCutInIsLoaded(pph, esc);

            var line = gev.EVESegment.GetLineById(rerouteFromLineId); //#55
            EVELine bodyLine = gev.EVESegment.InsertRerouteBlock(line, rerouteFromOpCodePos, gev.EVESegment.GetLineById(0x0037), true);

            bodyLine?.Body.Add(new EVEOpCode(bodyLine, 0x0003, 0x0000));

            EnsureImgCutInIsLoaded(pph, esc);
            EnsureImgCutIsGenerated(esc);

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

            foreach(var line in gev.EVESegment.Blocks.SelectMany(i => i.EVELines))
            {
                if (line.LineId == 0x003C && gevName == "ME09")
                {
                    string cutsceneName = "EVC_ST_035";
                    EVCSceneHandler esc = GetEscByName(cutsceneName);
                    ME09SpecialCase(pph, esc, gev, subtitleModelName);

                    continue;
                }

                var evcPlaybacks = line.ParsedCommands.OfType<EVCPlayback>();

                foreach(var evcPlayback in evcPlaybacks)
                {
                    var nextLine = gev.EVESegment.GetLineById((ushort)(line.LineId+1));
                    EVELine bodyLine = gev.EVESegment.InsertRerouteBlock(
                        gev.EVESegment.GetLineById(line.LineId),
                        evcPlayback.Pos,
                        nextLine, true);

                    EVCSceneHandler esc = GetEscByName(evcPlayback.Str);

                    EnsureImgCutInIsLoaded(pph, esc);
                    EnsureImgCutIsGenerated(esc);

                    PrepareEvcActors(gev, esc, bodyLine, subtitleModelName);

                    //TODO better OpCode clone code :D
                    bodyLine.Body.Add(new EVEOpCode(bodyLine, evcPlayback.OpCode.HighWord, evcPlayback.OpCode.LowWord));
                }
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
        }

        public static bool EnableImgCutInGeneration = true;
        public static ConcurrentHashSet<string> GeneratedImgCutIns = new ConcurrentHashSet<string>();
        public static void CreateImgCutIn(string voiceFile)
        {
            var pilotCodeOverride = SpecialCases.VoiceFileToAvatar.ContainsKey(voiceFile) ? SpecialCases.VoiceFileToAvatar[voiceFile] : null;
            if (EnableImgCutInGeneration && GeneratedImgCutIns.Add(voiceFile))
                Env.PrepSubGen().RepackSubtitleTemplate(voiceFile, pilotCodeOverride);
        }
    }
}
