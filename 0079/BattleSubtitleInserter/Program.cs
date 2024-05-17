using BinaryDataHelper;
using BinaryFile.Formats.Nintendo;
using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVECommands;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using ConcurrentCollections;
using R79JAFshared;
using System.Collections.Concurrent;
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

            var gen = new SubtitleImgCutInGenerator(
                @"C:\G\Wii\R79JAF patch assets\SubtitleAssets",
                @"C:\G\Wii\R79JAF_dirty\DATA\files\sound\stream",
                @"C:\G\Wii\R79JAF patch assets\subtitleTranslation",
                @"C:\G\Wii\R79JAF patch assets\tempDir"
                );

            var ctx = PrepXBFMarshaling(out var mXBF, out var mU8, out var mGEV);

            var gevTutprial = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\other", "TR*.gev");
            var gevAce = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\ace", "A*.gev");
            var gevEarth = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\efsf", "me*.gev");
            var gevZeon = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\zeon", "mz*.gev");

            var voiceFiles = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\sound\stream", "*.brstm")
                .Select(i => Path.GetFileNameWithoutExtension(i)).ToHashSet();

            var allGevs = gevTutprial.Concat(gevAce).Concat(gevEarth).Concat(gevZeon);

            ConcurrentHashSet<string> processedCutIns = new ConcurrentHashSet<string>();
            ConcurrentHashSet<string> processedEVC = new ConcurrentHashSet<string>();

            allGevs = allGevs.Where(i => i.Contains("AA01"));

            //foreach (var file in allGevs)
            Parallel.ForEach(allGevs, (file) =>
            {
                //HACK test on AA01 only for now
                //if (!file.Contains("AK01", StringComparison.InvariantCultureIgnoreCase)) continue;

                var gev = mGEV.Deserialize(null, null, File.ReadAllBytes(file).AsMemory(), ctx, out _);

                var referencedVoiceFiles = gev.STR
                    .Select((s, n) => new { s, n })
                    .Where(i => voiceFiles.Contains(i.s)).ToList();

                var prefetchLine = gev.EVESegment.Blocks[1].EVELines[0];

                foreach (var refVoice in referencedVoiceFiles)
                {
                    var sbytes = refVoice.s.ToBytes(Encoding.ASCII, fixedLength: 8);

                    prefetchLine.Body.InsertRange(1, [
                            //cutin/avatar load
                            new EVEOpCode(0x00A4FFFF),
                            //string
                            new EVEOpCode(sbytes.Take(4)),
                            new EVEOpCode(sbytes.Skip(4).Take(4)),
                            //load cutin only
                            new EVEOpCode(0x00000001),
                        ]);

                    prefetchLine.LineLengthOpCode.HighWord += 4;
                }

                foreach (var line in gev.EVESegment.Blocks.SelectMany(i => i.EVELines))
                {
                    var parsedCommands = line.ParsedCommands;

                    var voicePlaybacks = parsedCommands.OfType<VoicePlayback>();
                    var evcPlaybacks = parsedCommands.OfType<EVCPlayback>();

                    foreach (var evcPlayback in evcPlaybacks)
                    {
                        Console.WriteLine($"{line.LineId} {evcPlayback.Str}");

                        var evcPath = @"C:\G\Wii\R79JAF_clean\DATA\files\evc\" + evcPlayback.Str + ".arc";
                        var evcArc = mU8.Deserialize(null, null,
                            File.ReadAllBytes(evcPath),
                            ctx, out _);

                        var evcScene = evcArc["/arc/EvcScene.xbf"];
                        var xbf = (evcScene as U8FileNode).File as XBFFile;
                        var xmlstr = xbf.ToString();

                        var xml = xbf.ToXDocument();

                        //remove all existing CutIn calls
                        xml.XPathSelectElements("//ImgCutIn").Remove();
                        //and frame waits which hopefully are only for CutIns
                        xml.XPathSelectElements("//Frame").Remove();

                        //each Cut has separate Frame times and Unit list
                        foreach (var cut in xml.XPathSelectElements("//Cut"))
                        {
                            var voices = cut.XPathSelectElements("./Voice[text() != 'End']");
                            foreach (var voice in voices)
                            {
                                var voiceFile = voice.Value;

                                if (!voiceFiles.Contains(voiceFile)) continue;

                                var imgcutin = new XElement("ImgCutIn", "Unit00");
                                voice.AddBeforeSelf(imgcutin);

                                var waitSum = voice.XPathSelectElements("preceding-sibling::VoiceWait")
                                    .Select(i => float.Parse(i.Value))
                                    .Sum();

                                var voiceSum = voice.XPathSelectElements("preceding-sibling::Voice")
                                    .Select(i=>i.Value)
                                    .Where(i=>voiceFiles.Contains(i))
                                    .Select(i => ExternalToolsHelper.GetBRSTMduration($@"C:\G\Wii\R79JAF_clean\DATA\files\sound\stream\{i}.brstm"))
                                    .Sum() * 60;

                                var delay = waitSum + voiceSum;
                                delay = Math.Ceiling(delay);

                                var frameNode = new XElement("Frame", delay.ToString());
                                frameNode.Add(new XAttribute("type", "f32"));
                                imgcutin.AddBeforeSelf(frameNode);
                            }
                        }

                        if (processedEVC.Add(evcPath))
                        {
                            (evcScene as U8FileNode).File = new XBFFile(xml);
                            var bbbb = new ByteBuffer();
                            mU8.Serialize(evcArc, bbbb, ctx, out _);
                            File.WriteAllBytes(evcPath.Replace("_clean", "_dirty"), bbbb.GetData());
                        }
                    }

                    foreach (var voicePlayback in voicePlaybacks)
                    {
                        Console.WriteLine(voicePlayback.Str);
                        var index = parsedCommands.IndexOf(voicePlayback);

                        index++;
                        if (index < parsedCommands.Count && parsedCommands[index] is AvatarDisplay avatar)
                        {
                            var sbytes = voicePlayback.Str.ToBytes(Encoding.ASCII, fixedLength: 8);

                            Console.WriteLine(avatar.Str);

                            var pilotCodeOverride = avatar.Str.NullTrim();
                            //do not do face match for minions
                            if (pilotCodeOverride.StartsWith("sl", StringComparison.InvariantCultureIgnoreCase))
                                pilotCodeOverride = null;

                            //do not process same file multiple times, one voice file = one avatar
                            //if(processedCutIns.Add(voicePlayback.Str))
                            //  gen.RepackSubtitleTemplate(voicePlayback.Str, @"C:\G\Wii\R79JAF_dirty\DATA\files\_2d\ImageCutIn", pilotCodeOverride);

                            //update avatar.Str to match voice.Str
                            line.Body[avatar.Pos + 1] = new EVEOpCode(sbytes.Take(4));
                            line.Body[avatar.Pos + 2] = new EVEOpCode(sbytes.Skip(4).Take(4));
                            //display ImgCutIn instead of MsgBox
                            line.Body[avatar.Pos + 3] = new EVEOpCode(0x0002FFFF);
                        }
                    }
                }

                var outputFile = file.Replace("_clean", "_dirty");
                var bb = new ByteBuffer();
                mGEV.Serialize(gev, bb, ctx, out _);
                File.WriteAllBytes(outputFile, bb.GetData());
            });
        }

        private static IMarshalingContext PrepXBFMarshaling(out ITypeMarshaler<XBFFile> mXBF, out ITypeMarshaler<U8File> mU8
            , out ITypeMarshaler<GEV> mGEV)
        {
            var store = new DefaultMarshalerStore();
            var rootCtx = new RootMarshalingContext(store);

            U8Marshaling.Register(store);
            XBFMarshaling.Register(store);
            GEVMarshaling.Register(store);

            mXBF = store.FindMarshaler<XBFFile>();

            mU8 = store.FindMarshaler<U8File>();

            mGEV = store.FindMarshaler<GEV>();

            return rootCtx;
        }
    }
}
