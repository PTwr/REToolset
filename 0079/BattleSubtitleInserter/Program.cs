using BinaryDataHelper;
using BinaryFile.Formats.Nintendo;
using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVECommands;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVELines;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using ConcurrentCollections;
using R79JAFshared;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BattleSubtitleInserter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //TODO iterate mission .gev
            //TODO locate all sound strings in STR
            //TODO insert all matching subtitle .brres into prefetch block
            //TODO find voice invocation in EVE
            //TODO get actor from voice invocation
            //TODO generate Subtitle brres/arc with matching face

            var gen = Env.PrepSubGen();

            var ctx = MarshalingHelper.PrepXBFMarshaling(out var mXBF, out var mU8, out var mGEV);

            var gevTutprial = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\other", "TR*.gev");
            var gevAce = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\ace", "A*.gev");
            var gevEarth = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\efsf", "me*.gev");
            var gevZeon = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\zeon", "mz*.gev");

            var voiceFiles = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\sound\stream", "*.brstm")
                .Select(i => Path.GetFileNameWithoutExtension(i)).ToHashSet();

            var allGevs = gevTutprial.Concat(gevAce).Concat(gevEarth).Concat(gevZeon);

            {
                Env.PrepSubGen().EnableDebugToolTip = true;
                Env.PrepSubGen().MtlWarning = true;
                Env.PrepSubGen().ExcludeMtl = true;
                Env.ReadFFProbeCache();

                Subtitler.EnableImgCutInGeneration = true;
                Subtitler.EnableGevUnpacking = true;
                Subtitler.CombineSubtitles = true;

                var bootArc = mU8.Deserialize(null, null, File.ReadAllBytes(Env.BootArcAbsolutePath()).AsMemory(), ctx, out _);
                var pph = new PilotParamHandler(bootArc);

                foreach (var file in allGevs
                    .Where(f => f.Contains("ME01", StringComparison.InvariantCultureIgnoreCase))
                    )
                {
                    Console.WriteLine("-------------------------------------------------------------------------");
                    Console.WriteLine(file);
                    Console.WriteLine("-------------------------------------------------------------------------");
                    Subtitler.Subtitle(file, pph);
                }

                Subtitler.Save(pph, Env.BootArcAbsolutePath());
                Env.SaveFFProbeCache();

                File.WriteAllLines(Env.PatchAssetDirectory + "/VoiceFilesInUse.txt", Env.PrepSubGen().UsedVoiceFiles.Order());
                File.WriteAllLines(Env.PatchAssetDirectory + "/MissingAvatar.txt", Env.PrepSubGen().FacelessVoiceFiles.Order());
            }
        }
    }
}
