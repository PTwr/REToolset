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
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BattleSubtitleInserter
{
    internal class Program
    {
        static string VoiceFileToPilotPram(string voiceFile)
        {
            //unused prefix
            var result = "Z";

            var group = voiceFile
                .Where(char.IsLetter);

            //recode voice file number to letters to not trip pilot-variant mechanism
            var number = voiceFile
                .Where(char.IsNumber)
                .Select(i => (char)('A' + (i - 48)));

            result = new string(result.Concat(group).Concat(number).ToArray()).ToUpper();

            return result;
        }
        static void Main(string[] args)
        {
            byte[] xx = [0x50, 0x4C, 0x5F, 0x47, 0x41, 0x57, 0x00, 0x00];
            var ssss = xx.AsSpan()
                .ToDecodedString(BinaryStringHelper.Shift_JIS);

            Dictionary<string, string> voiceFileToAvatar = new Dictionary<string, string>()
            {
                {"eva001", "amr" },
                {"eva002", "kai" },
                {"eva003", "hyt" },
            };

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

            var bootU8 = mU8.Deserialize(null, null, File.ReadAllBytes(@"C:\G\Wii\R79JAF_clean/DATA\files\boot\boot.arc").AsMemory(), ctx, out _);
            var pilotParamXml = ((bootU8["/arc/pilot_param.xbf"] as U8FileNode).File as XBFFile).ToXDocument();

            var gevTutprial = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\other", "TR*.gev");
            var gevAce = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\ace", "A*.gev");
            var gevEarth = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\efsf", "me*.gev");
            var gevZeon = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\zeon", "mz*.gev");

            var voiceFiles = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\sound\stream", "*.brstm")
                .Select(i => Path.GetFileNameWithoutExtension(i)).ToHashSet();

            var allGevs = gevTutprial.Concat(gevAce).Concat(gevEarth).Concat(gevZeon);

            ConcurrentHashSet<string> processedCutIns = new ConcurrentHashSet<string>();
            ConcurrentHashSet<string> processedEVC = new ConcurrentHashSet<string>();
            ConcurrentHashSet<string> processedPilotParam = new ConcurrentHashSet<string>();

            //allGevs = allGevs.Where(i => i.Contains("me02", StringComparison.InvariantCultureIgnoreCase));

            //{
            //    var ff = @"C:\G\Wii\R79JAF_dirty\DATA\files\_2d\ImageCutIn\IC_CHR.arc";
            //    var evc = mU8.Deserialize(null, null, File.ReadAllBytes(ff).AsMemory(), ctx, out _);
            //    var xbf = (evc["/arc/model_.xbf"] as U8FileNode).File as XBFFile;
            //    var xml = xbf.ToXDocument().ToString();


            //    File.WriteAllText(ff + ".txt", xml);



            //    (evc["/arc/model_.xbf"] as U8FileNode).File = new XBFFile(XDocument.Load(ff + ".txt"));

            //    var bb = new ByteBuffer();
            //    mU8.Serialize(evc, bb, ctx, out _);

            //    File.WriteAllBytes(ff, bb.GetData());

            //    return;
            //}

            foreach (var ff in Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\evc", "EVC*.arc"))
            {
                break;

                var evc = mU8.Deserialize(null, null, File.ReadAllBytes(ff).AsMemory(), ctx, out _);
                var xbf = (evc["/arc/EvcScene.xbf"] as U8FileNode).File as XBFFile;
                var xbfDebug = (evc["/arc/EvcDebug.xbf"] as U8FileNode).File as XBFFile;

                var xml = xbf.ToXDocument().ToString();
                var xmlDebug = xbfDebug.ToXDocument().ToString();

                //File.WriteAllText(ff.Replace("_clean", "_dirty") + ".txt", xml);
                File.WriteAllText(ff.Replace("_clean", "_dirty") + ".EvcDebug.txt", xmlDebug);

                foreach (var node in (evc["/arc"] as U8DirectoryNode).Children.OfType<U8FileNode>())
                {
                    var cut = node.File as U8File;

                    if (cut is null) continue;

                    var cutxbf = (cut["/arc/EvcCut.xbf"] as U8FileNode).File as XBFFile;
                    xml = cutxbf.ToXDocument().ToString();

                    File.WriteAllText(ff.Replace("_clean", "_dirty") + "__" + node.Name + ".clean.txt", xml);

                    (cut["/arc/EvcCut.xbf"] as U8FileNode).File = new XBFFile(
                        XDocument.Load(ff.Replace("_clean", "_dirty") + "__" + node.Name + ".txt"));
                }

                (evc["/arc/EvcScene.xbf"] as U8FileNode).File = new XBFFile(
                    XDocument.Load(ff.Replace("_clean", "_dirty") + ".txt"));

                var bb = new ByteBuffer();

                mU8.Serialize(evc, bb, ctx, out _);

                File.WriteAllBytes(ff.Replace("_clean", "_dirty"), bb.GetData());
            }
            //return;

            bool generateImgCutIn = true;

            ParallelOptions opt = new ParallelOptions()
            {
                //MaxDegreeOfParallelism = 1,
            };

            //foreach (var file in allGevs)
            Parallel.ForEach(allGevs, opt, (file) =>
            {
                Console.WriteLine(file);

                var gev = mGEV.Deserialize(null, null, File.ReadAllBytes(file).AsMemory(), ctx, out _);

                var referencedVoiceFiles = gev.STR
                    .Select((s, n) => new { s, n })
                    .Where(i => voiceFiles.Contains(i.s)).ToList();

                var prefetchLine = gev.EVESegment.Blocks[1].EVELines[0];

                //try to reuse any loaded weapon
                var subtitlesObjectName = prefetchLine
                    .ParsedCommands
                    .OfType<ResourceLoad>()
                    .Where(i => i.ResourceName.StartsWith("WP_"))
                    .Select(i => i.ResourceName)
                    .FirstOrDefault();

                //if none found, insert random weapon to prevent access violation exceptions
                if (subtitlesObjectName is null)
                {
                    subtitlesObjectName = "WP_EXA";

                    var sbytes = subtitlesObjectName.ToBytes(Encoding.ASCII, fixedLength: 8);

                    prefetchLine.Body.InsertRange(1, [
                            //cutin/avatar load
                            new EVEOpCode(0x004BFFFF),
                            //string
                            new EVEOpCode(sbytes.Take(4)),
                            new EVEOpCode(sbytes.Skip(4).Take(4)),
                        ]);
                    prefetchLine.LineLengthOpCode.HighWord += 3;
                }

                var subtitlesObjectBytes = subtitlesObjectName.ToBytes(Encoding.ASCII, fixedLength: 8);

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

                        var referencingLine = gev.EVESegment.Blocks.SelectMany(i => i.EVELines)
                            .Where(i => i.ParsedCommands.OfType<Jump>().LastOrDefault()?.TargetLineId == line.LineId)
                            .FirstOrDefault();

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
                            var cutAnimArc = (evcArc["/arc/" + cut.XPathSelectElement("./File").Value] as U8FileNode).File as U8File;
                            var cutAnimxbf = (cutAnimArc["/arc/EvcCut.xbf"] as U8FileNode).File as XBFFile;
                            var cutEvcUnitDocument = cutAnimxbf.ToXDocument();
                            var cutEvcUnitXml = cutEvcUnitDocument.XPathSelectElement("//EvcUnit");
                            if (cutEvcUnitXml is null)
                            {
                                cutEvcUnitXml = new XElement("EvcUnit");
                                cutEvcUnitDocument.Root.Add(cutEvcUnitXml);
                            }

                            //var txtPath = evcPath.Replace("_clean", "_dirty") + "__" + cut.XPathSelectElement("./File").Value + ".txt";

                            //TODO update Cut to match EvcScene actors
                            //var cutXml = XDocument.Load(txtPath);

                            //(cutArc["/arc/EvcCut.xbf"] as U8FileNode).File = new XBFFile(cutXml);

                            ushort subtitleId = 0;

                            var voices = cut.XPathSelectElements("./Voice[text() != 'End']");
                            foreach (var voice in voices)
                            {
                                var voiceFile = voice.Value;

                                if (!voiceFiles.Contains(voiceFile)) continue;

                                var pilotParamCode = VoiceFileToPilotPram(voiceFile);
                                if (processedPilotParam.Add(pilotParamCode))
                                {
                                    Console.WriteLine($"Adding Pilot Pram '{pilotParamCode}'");

                                    var pilotNode = new XElement("PILOT");
                                    pilotNode.Add(new XAttribute("type", "FORMAT"));
                                    pilotNode.Add(new XAttribute("name", pilotParamCode));
                                    var voiceNode = new XElement("VOICE", voiceFile);
                                    voiceNode.Add(new XAttribute("type", "string"));
                                    pilotNode.Add(voiceNode);

                                    pilotParamXml.Root.Add(pilotNode);

                                    var pilotCodeOverride = voiceFileToAvatar.ContainsKey(voiceFile) ? voiceFileToAvatar[voiceFile] : null;
                                    if(processedCutIns.Add(voiceFile) && generateImgCutIn)
                                      gen.RepackSubtitleTemplate(voiceFile, @"C:\G\Wii\R79JAF_dirty\DATA\files\_2d\ImageCutIn", pilotCodeOverride);
                                }

                                //TODO use PilotParam code instead?
                                ushort ofsId = (ushort)gev.STR.IndexOf(voiceFile);
                                if (ofsId == 65535)
                                {
                                    gev.STR.Add(voiceFile);
                                    ofsId = (ushort)(gev.STR.Count - 1);
                                }

                                //TODO add Pilot Params
                                //TODO then load Pilot Param in referencing line (or try to do it from EVC invocation line? maybe it will work)
                                //can't rebind existing mech or positions will get fucked up, gotta create Actor per Subtitle


                                var sbytes = pilotParamCode.ToBytes(Encoding.ASCII, fixedLength: 8);

                                line.Body.InsertRange(
                                    1,
                                    //line.Body.Count - 1, 
                                    [
                                    new EVEOpCode(0x00FA, gev.GetOrInsertId($"SUBTITLE_{subtitleId:D2}")), //TODO load Pilot Param and pass its id here
                                    //new EVEOpCode(gev.GetOrInsertId($"Other{subtitleId}"), 0xFFFF) //eva*** - to make ImgCutIn and Voice match in EvcScene
                                    new EVEOpCode(gev.GetOrInsertId($"SUB{subtitleId:D2}"), 0xFFFF) //eva*** - to make ImgCutIn and Voice match in EvcScene
                                    ]);
                                line.LineLengthOpCode.HighWord += 2;

                                ///////////////////////////////////


                                line.Body.InsertRange(
                                    //referencingLine.Body.Count - 1 - 3,
                                    //6,
                                    1,
                                    [
                                    new EVEOpCode(0x0056, gev.GetOrInsertId($"SUBTITLE_{subtitleId:D2}")),
                                    new EVEOpCode(subtitlesObjectBytes.Take(4)), //first 4 chars of random weapon code
                                    new EVEOpCode(subtitlesObjectBytes.Skip(4)), //second 4 chars of random weapon code
                                    new EVEOpCode(0x82C882B5), // なし (none) as attachment/position
                                    new EVEOpCode(0x00000000), //unused 4 chars of attachment string
                                    
                                    new EVEOpCode(0x00000001), //unused 4 chars of attachment string
                                    new EVEOpCode(0x0001, gev.GetOrInsertId($"SUBTITLE_{subtitleId:D2}")), //unused 4 chars of attachment string
                                    new EVEOpCode(0x00020000), //unused 4 chars of attachment string

                                    //crashes with ofs->evexxx but works with ofs->Boss0?
                                    new EVEOpCode(0x006A, gev.GetOrInsertId($"SUBTITLE_{subtitleId:D2}")), //pilot param bound to voice file name
                                    new EVEOpCode(sbytes.Take(4)), //first 4 chars of pilot param code
                                    new EVEOpCode(sbytes.Skip(4)), //second 4 chars of pilot param code
                                    new EVEOpCode(0x82C882B5), // なし (none) as attachment/position
                                    new EVEOpCode(0x00000000), //unused 4 chars of attachment string
                                    ]);

                                //object load/spawn
                                line.LineLengthOpCode.HighWord += 5 + 3;
                                //pilot param ref
                                line.LineLengthOpCode.HighWord += 5;

                                ///////////////////////////////////

                                //TODO do something with crash after EVC if PilotParams get loaded

                                //var unbindLine = line.Parent.EVELines[1];
                                //TODO add Unbind in next line if needed

                                sbytes = voiceFile.ToBytes(Encoding.ASCII, fixedLength: 8);
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

                                var unitNode = new XElement("Unit");
                                //TODO generate GEV actor
                                unitNode.Add(new XElement("ScnName", $"SUB{subtitleId:D2}"));

                                var evcName = $"Sub{subtitleId:D2}";
                                unitNode.Add(new XElement("EvcName", evcName));

                                var evcCutUnit = cutEvcUnitXml.XPathSelectElement($".//Name[text() = '{evcName}']");
                                if (evcCutUnit is null)
                                {;
                                    cutEvcUnitXml.Add(new XElement("Unit", new XElement("Name", evcName)));
                                }

                                cut.Add(unitNode);

                                //var imgcutin = new XElement("ImgCutIn", $"Unit{subtitleId:D2}");
                                var imgcutin = new XElement("ImgCutIn", evcName);
                                voice.AddBeforeSelf(imgcutin);

                                var waitSum = voice.XPathSelectElements("preceding-sibling::VoiceWait")
                                    .Select(i => float.Parse(i.Value))
                                    .Sum();

                                var voiceSum = voice.XPathSelectElements("preceding-sibling::Voice")
                                    .Select(i => i.Value)
                                    .Where(i => voiceFiles.Contains(i))
                                    .Select(i => ExternalToolsHelper.GetBRSTMduration($@"C:\G\Wii\R79JAF_clean\DATA\files\sound\stream\{i}.brstm"))
                                    .Sum() * 60;

                                var delay = waitSum + voiceSum;
                                delay = Math.Ceiling(delay);

                                var frameNode = new XElement("Frame", delay.ToString());
                                frameNode.Add(new XAttribute("type", "f32"));
                                imgcutin.AddBeforeSelf(frameNode);

                                subtitleId++;
                            }

                            (cutAnimArc["/arc/EvcCut.xbf"] as U8FileNode).File = new XBFFile(cutEvcUnitXml.Document);
                        }

                        if (processedEVC.Add(evcPath))
                        {
                            var customXmlPath = evcPath.Replace("_clean", "_dirty") + ".txt";

                            File.WriteAllText(customXmlPath + ".txt", xml.ToString());

                            //xml = XDocument.Load(evcPath.Replace("_clean", "_dirty") + ".txt");
                            (evcScene as U8FileNode).File = new XBFFile(xml);
                            var bbbb = new ByteBuffer();
                            mU8.Serialize(evcArc, bbbb, ctx, out _);
                            File.WriteAllBytes(evcPath.Replace("_clean", "_dirty"), bbbb.GetData());

                            //File.WriteAllText(evcPath.Replace("_clean", "_dirty") + ".txt", xml.ToString());
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

                            //hardcoded list of avatars for voice lines that can't be automatched
                            pilotCodeOverride = voiceFileToAvatar.ContainsKey(voicePlayback.Str) ? voiceFileToAvatar[voicePlayback.Str] : pilotCodeOverride;

                            //do not process same file multiple times, one voice file = one avatar
                            if(processedCutIns.Add(voicePlayback.Str) && generateImgCutIn)
                              gen.RepackSubtitleTemplate(voicePlayback.Str, @"C:\G\Wii\R79JAF_dirty\DATA\files\_2d\ImageCutIn", pilotCodeOverride);

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

                R79JAFshared.GEVUnpacker.UnpackGev(ctx, mGEV, outputFile, outputFile.Replace(".gev", "_mod").Replace("_clean", "_dirty"));
            });

            (bootU8["/arc/pilot_param.xbf"] as U8FileNode).File = new XBFFile(pilotParamXml);

            var b = new ByteBuffer();
            mU8.Serialize(bootU8, b, ctx, out _);

            File.WriteAllBytes(@"C:\G\Wii\R79JAF_dirty\DATA\files\boot\boot.arc", b.GetData());
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
