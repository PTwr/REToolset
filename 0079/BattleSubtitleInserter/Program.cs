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

            //There is limit of 7 chars? 8th char has to be null?
            if (voiceFile.EndsWith('b'))
            {
                //voiceFile = "eve999";
                voiceFile = voiceFile.TrimEnd('b');
                result = "X";
            }

            var group = voiceFile
                .Take(3) //max3
                .Where(char.IsLetter);

            //recode voice file number to letters to not trip pilot-variant mechanism
            var number = voiceFile
                .Where(char.IsNumber)
                .Select(i => (char)('A' + (i - 48)));

            result = new string(result.Concat(group).Concat(number).ToArray()).ToUpper();

            //var suffix = voiceFile
            //    .Skip(group.Count())
            //    .Skip(number.Count());

            //if (suffix.Count() > 1) throw new Exception($"Unsupported format of voice filename of '{voiceFile}'");

            //if (suffix.Count() == 1)
            //{
            //    result += new string(suffix.ToArray()).ToUpper();
            //}

            return result;
        }
        static void Main(string[] args)
        {
            byte[] xx = [0x50, 0x4C, 0x5F, 0x47, 0x41, 0x57, 0x00, 0x00];
            var ssss = xx.AsSpan()
                .ToDecodedString(BinaryStringHelper.Shift_JIS);
            byte[] xxx = [0x48, 0x4D, 0x5F, 0x4B, 0x41, 0x49, 0x00, 0x00];
            var sssss = xxx.AsSpan()
                .ToDecodedString(BinaryStringHelper.Shift_JIS);
            byte[] xxxx = [0x48, 0x4D, 0x5F, 0x48, 0x59, 0x54, 0x00, 0x00];
            var ssssss = xxxx.AsSpan()
                .ToDecodedString(BinaryStringHelper.Shift_JIS);
            byte[] xxxxx = [0x82, 0xC8, 0x82, 0xB5, 0x00, 0x00, 0x00, 0x00];
            var sssssss = xxxxx.AsSpan()
                .ToDecodedString(BinaryStringHelper.Shift_JIS);

            Dictionary<string, string> voiceFileToAvatar = new Dictionary<string, string>()
            {
                //AA01
                { "eva001", "amr" },
                { "eva002", "kai" },
                { "eva003", "hyt" },
                { "eva004", "chr" },
                { "eva005", "amr" },
                { "eva091", "chr" },
                //AA02
                { "eva006", "amr" },
                { "eva007", "kai" },
                //2nd cutscene
                { "eva009", "chr" },
                { "eva010", "amr" },
                { "eva011", "amr" },

                //ME01
                { "eve010", "aln" }, //intro
                //EVC_ST_006
                { "eve020", "hoa" },
                { "eve021", "aln" },
                { "eve022", "lls" },
                { "eve023", "dnb" },
                { "eve024", "aln" },
                { "eve025", "lls" },
                { "eve026", "aln" },
                { "eve543", "hoa" }, //reusable voice line?
                //EVC_ST_007 ending
                { "eve027", "aln" },
                { "eve028", "lls" },
                { "eve029", "hoa" },
                { "eve030", "lls" },
                { "eve031", "dnb" },
                { "eve032", "aln" },

                //ME05
                { "eve110", "aln" }, //first faceless voice

                //ME15
                //EVE after EVC_ST_061 (third cutscene)
                //EVE has disabled LLN avatar for most voice lines :D
                { "eve595", "hoa" }, //or maybe Lily? listen moar
                { "eve596", "aln" },
                { "slb097", "hu1" }, //TODO show EFF or Zeon minion for traitor/assasin?
                { "eve597", "aln" },
                { "slb098", "hu1" }, //TODO show EFF or Zeon minion for traitor/assasin?
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

            allGevs = allGevs.Where(i =>
                //i.Contains("me01", StringComparison.InvariantCultureIgnoreCase) ||
                //Path.GetFileNameWithoutExtension(i).StartsWith("me11", StringComparison.InvariantCultureIgnoreCase) ||
                //Path.GetFileNameWithoutExtension(i).StartsWith("me21", StringComparison.InvariantCultureIgnoreCase) ||
                Path.GetFileNameWithoutExtension(i).StartsWith("me05", StringComparison.InvariantCultureIgnoreCase)
                );

            bool generateImgCutIn = false;

            ParallelOptions opt = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 1,
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
                //subtitlesObjectName = null;
                //if none found, insert random weapon to prevent access violation exceptions
                if (subtitlesObjectName is null && false)
                {
                    subtitlesObjectName = "WP_EXA";

                    var sbytes = subtitlesObjectName.ToBytes(Encoding.ASCII, fixedLength: 8);

                    prefetchLine.Body.InsertRange(prefetchLine.Body.Count - 1, [
                            //cutin/avatar load
                            new EVEOpCode(0x004BFFFF),
                            //string
                            new EVEOpCode(sbytes.Take(4)),
                            new EVEOpCode(sbytes.Skip(4).Take(4)),
                        ]);
                    prefetchLine.LineLengthOpCode.HighWord += 3;
                }
                subtitlesObjectName = "OB_CNE";

                var subtitlesObjectBytes = subtitlesObjectName.ToBytes(Encoding.ASCII, fixedLength: 8);

                //referencedVoiceFiles.Clear();

                referencedVoiceFiles = referencedVoiceFiles
                    //.Skip(7)
                    //.Skip(8)
                    //.Take(8)
                    .ToList();

                HashSet<string> prefetchedImgCutIns = new HashSet<string>();
                foreach (var refVoice in referencedVoiceFiles)
                {
                    //if GEV invocation is not found, lack of on-demand generated ImgCutIn will crash the game :D
                    //var sbytes = refVoice.s.ToBytes(Encoding.ASCII, fixedLength: 8);

                    //if (true)
                    //{
                    //    Console.WriteLine($"Prefetching ImgCutIn for '{refVoice.s}'");

                    //    prefetchLine.Body.InsertRange(1, [
                    //        //cutin/avatar load
                    //        new EVEOpCode(0x00A4FFFF),
                    //        //string
                    //        new EVEOpCode(sbytes.Take(4)),
                    //        new EVEOpCode(sbytes.Skip(4).Take(4)),
                    //        //load cutin only
                    //        new EVEOpCode(0x00000001),
                    //    ]);
                    //    prefetchLine.LineLengthOpCode.HighWord += 4;


                    //}
                }

                ushort subtitleId = 0;
                //materialization required as Line addition will change source collection
                foreach (var line in gev.EVESegment.Blocks.SelectMany(i => i.EVELines).ToList())
                {
                    var parsedCommands = line.ParsedCommands;

                    var voicePlaybacks = parsedCommands.OfType<VoicePlayback>();
                    var evcPlaybacks = parsedCommands.OfType<EVCPlayback>();

                    var facelessPlaybacks = parsedCommands.OfType<FacelessVoicePlayback>();

                    //evcPlaybacks = Enumerable.Empty<EVCPlayback>();
                    //voicePlaybacks = Enumerable.Empty<VoicePlayback>();

                    if (facelessPlaybacks.Any())
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Updating Faceless Playback line 0x{line.LineId:X4} #{line.LineId}");
                    }
                    foreach (var facelessPlayback in facelessPlaybacks)
                    {
                        var index = facelessPlayback.Pos;
                        Console.WriteLine($"EVE {line.Body[facelessPlayback.Pos]} {facelessPlayback.Str}");

                        var sbytes = facelessPlayback.Str.ToBytes(Encoding.ASCII, fixedLength: 8);

                        if (true && prefetchedImgCutIns.Add(facelessPlayback.Str))
                        {
                            sbytes = facelessPlayback.Str.ToBytes(Encoding.ASCII, fixedLength: 8);
                            Console.WriteLine($"Prefetching ImgCutIn for '{facelessPlayback.Str}'");

                            prefetchLine.Body.InsertRange(
                                prefetchLine.Body.Count - 1
                                , [
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

                        var pilotCodeOverride = facelessPlayback.Str.Substring(0, 3).NullTrim();
                        //do not do face match for minions
                        if (pilotCodeOverride.StartsWith("sl", StringComparison.InvariantCultureIgnoreCase))
                            pilotCodeOverride = null;

                        //hardcoded list of avatars for voice lines that can't be automatched
                        pilotCodeOverride = voiceFileToAvatar.ContainsKey(facelessPlayback.Str) ? voiceFileToAvatar[facelessPlayback.Str] : pilotCodeOverride;

                        //do not process same file multiple times, one voice file = one avatar
                        if (processedCutIns.Add(facelessPlayback.Str) && generateImgCutIn)
                            gen.RepackSubtitleTemplate(facelessPlayback.Str, @"C:\G\Wii\R79JAF_dirty\DATA\files\_2d\ImageCutIn", pilotCodeOverride);

                        Console.WriteLine($"Inserting Avatar display for {facelessPlayback.Str} with {pilotCodeOverride}");

                        var originalLine = line;
                        {
                            var jumpTable = gev.EVESegment.Blocks[0].EVELines[0] as EVEJumpTable;

                            var newBlock = new EVEBlock(gev.EVESegment);
                            gev.EVESegment.Blocks.Add(newBlock);
                            newBlock.Terminator = new EVEOpCode(0x0005, 0xFFFF);

                            var newLine = new EVELine(newBlock);
                            newBlock.EVELines = [newLine];

                            //lineId should get autocalculated during serialization
                            newLine.LineStartOpCode = new EVEOpCode(newLine, 0x0001, 0x0000);
                            //3 opcodes for header and footer, and 1 for EVC Playback, 0x0002 is unknown parameter
                            newLine.LineLengthOpCode = new EVEOpCode(newLine, 3, 0x0002);

                            newLine.Terminator = new EVEOpCode(0x0004, 0x0000);

                            newLine.Body = [
                                //avatar voice playback
                                new EVEOpCode(0x11B, facelessPlayback.OfsId),
                                new EVEOpCode(0x000A, 0xFFFF),

                                new EVEOpCode(0x40A0, 0x0000),
                                new EVEOpCode(sbytes.Take(4)),
                                new EVEOpCode(sbytes.Skip(4).Take(4)),
                                new EVEOpCode(0x0002, 0xFFFF),
                                ];

                            newLine.LineLengthOpCode.HighWord = (ushort)(newLine.Body.Count + 3);

                            ////////////

                            {
                                var jumpId = (ushort)jumpTable.AddJump(newLine);

                                //next line - won't work in tutorial
                                //TODO try to jump to next opcode instead of line
                                var returnLine = gev.EVESegment.Blocks
                                    .SelectMany(i => i.EVELines)
                                    .Where(i => i.LineId == line.LineId + 1)
                                    .FirstOrDefault();

                                var returnJumpId = (ushort)jumpTable.AddJump(returnLine);

                                newLine.Body.Add(new EVEOpCode(newLine, 0x0011, returnJumpId));
                                newLine.LineLengthOpCode.HighWord++;

                                line.Body[facelessPlayback.Pos] = new EVEOpCode(line, 0x0011, jumpId);
                                line.Body[facelessPlayback.Pos+1] = new EVEOpCode(0);
                                line.Body[facelessPlayback.Pos+2] = new EVEOpCode(0);
                                line.Body[facelessPlayback.Pos+3] = new EVEOpCode(0);
                            }

                        }
                    }

                    if (evcPlaybacks.Any())
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Updating Cutscene Playback line 0x{line.LineId:X4} #{line.LineId}");
                    }
                    foreach (var evcPlayback in evcPlaybacks)
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine($"Line 0x{line.LineId:X4} {evcPlayback.Str}");

                        var originalLine = line;
                        var mechSpawnLine = line;
                        if (true)
                        {
                            var jumpTable = gev.EVESegment.Blocks[0].EVELines[0] as EVEJumpTable;

                            var newBlock = new EVEBlock(gev.EVESegment);
                            gev.EVESegment.Blocks.Add(newBlock);
                            newBlock.Terminator = new EVEOpCode(0x0005, 0xFFFF);

                            var newLine = new EVELine(newBlock);
                            newBlock.EVELines = [newLine];

                            //lineId should get autocalculated during serialization
                            newLine.LineStartOpCode = new EVEOpCode(newLine, 0x0001, 0x0000);
                            //3 opcodes for header and footer, and 1 for EVC Playback, 0x0002 is unknown parameter
                            newLine.LineLengthOpCode = new EVEOpCode(newLine, 3, 0x0002);

                            //fill body with just EVC Playback
                            Console.WriteLine($"Relocating EVC Playback command '{line.Body[evcPlayback.Pos]}'");
                            newLine.Body = [
                                new EVEOpCode(newLine, line.Body[evcPlayback.Pos].HighWord, evcPlayback.OfsId)
                                ];
                            newLine.LineLengthOpCode.HighWord++;

                            ushort jumpId = 0;
                            ushort returnJumpId = 0;
                            {
                                jumpId = (ushort)jumpTable.AddJump(newLine);

                                var returnLine = gev.EVESegment.Blocks
                                    .SelectMany(i => i.EVELines)
                                    .Where(i => i.LineId == line.LineId + 1)
                                    .FirstOrDefault();

                                //TODO test!!! Worked mostly fine when returned to line instead of returnLine o_O
                                returnJumpId = (ushort)jumpTable.AddJump(returnLine);
                            }

                            newLine.Body.Add(new EVEOpCode(newLine, 0x0011, returnJumpId));
                            newLine.LineLengthOpCode.HighWord++;

                            {
                                line.Body[evcPlayback.Pos] = new EVEOpCode(line, 0x0011, jumpId);
                            }

                            //TODO is return jump required? Or does 0x0005FFFF work as a return?

                            //perform all insertions of newly created line

                            newLine.Terminator = new EVEOpCode(0x0004, 0x0000);

                            mechSpawnLine = newLine;
                        }

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

                        var dumpXmplPath = evcPath.Replace("_clean", "_dirty") + ".txt";
                        File.WriteAllText(dumpXmplPath, xml.ToString());

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

                            var cutEvcTxtPath = evcPath.Replace("_clean", "_dirty") + "__" + cut.XPathSelectElement("./File").Value + ".txt";
                            File.WriteAllText(cutEvcTxtPath, cutEvcUnitDocument.ToString());

                            var cutEvcUnitXml = cutEvcUnitDocument.XPathSelectElement("//EvcUnit");
                            if (cutEvcUnitXml is null)
                            {
                                cutEvcUnitXml = new XElement("EvcUnit");
                                cutEvcUnitDocument.Root.Add(cutEvcUnitXml);
                            }

                            //TODO update Cut to match EvcScene actors
                            //var cutXml = XDocument.Load(txtPath);

                            //(cutArc["/arc/EvcCut.xbf"] as U8FileNode).File = new XBFFile(cutXml);

                            var voices = cut.XPathSelectElements("./Voice[text() != 'End']");
                            foreach (var voice in voices)
                            {
                                var voiceFile = voice.Value;

                                if (!voiceFiles.Contains(voiceFile)) continue;

                                var pilotParamCode = VoiceFileToPilotPram(voiceFile);
                                if (processedPilotParam.Add(pilotParamCode))
                                {
                                    Console.WriteLine($"Adding Pilot Pram '{pilotParamCode}' for voice '{voiceFile}'");

                                    var pilotNode = new XElement("PILOT");
                                    pilotNode.Add(new XAttribute("type", "FORMAT"));
                                    pilotNode.Add(new XAttribute("name", pilotParamCode));
                                    var voiceNode = new XElement("VOICE", voiceFile);
                                    voiceNode.Add(new XAttribute("type", "string"));
                                    pilotNode.Add(voiceNode);

                                    //all values are not needed when not spawning mech for placeholder AI
                                    if (false)
                                    {
                                        var existingPilotParam =
                                            pilotParamXml.XPathSelectElement("//PILOT[@name = 'HM_DEFAULT']");

                                        //deep clone
                                        pilotNode = new XElement(existingPilotParam);
                                        //and update important fields
                                        pilotNode.XPathSelectElement(".//VOICE").Value = voiceFile;
                                        pilotNode.Attribute("name").Value = pilotParamCode;
                                    }

                                    pilotParamXml.Root.Add(pilotNode);

                                    var pilotCodeOverride = voiceFileToAvatar.ContainsKey(voiceFile) ? voiceFileToAvatar[voiceFile] : null;
                                    if (processedCutIns.Add(voiceFile) && generateImgCutIn)
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

                                //TODO handle a/b suffix, eg. eve144b in ME06 EVC_ST_023
                                var sbytes = pilotParamCode.ToBytes(Encoding.ASCII, fixedLength: 8);

                                //AA01 seems to have no issue with inserts right into EVC call line
                                //but AA02 goes nuts, ME01 Gaw cutscene crashes game, and AA05 does not load?
                                //var mechSpawnLine = gev.EVESegment.Blocks[2].EVELines.First();
                                //mechSpawnLine = line.Parent.EVELines.First();
                                //mechSpawnLine = line;



                                //inserting anything but nulls in AA02 causes issues?
                                //maybe same issue as in ME01 Gaw cutscene?
                                //something wrong with modyfying multi-cut cutscenes?

                                //line.Body.Insert(1, new EVEOpCode(0x0));
                                //line.LineLengthOpCode.HighWord += 1;
                                //line.Body.Insert(1, new EVEOpCode(0x0));
                                //line.LineLengthOpCode.HighWord += 1;
                                //line.Body.Insert(1, new EVEOpCode(0x0));
                                //line.LineLengthOpCode.HighWord += 1;
                                //line.Body.Insert(1, new EVEOpCode(0x0));
                                //line.LineLengthOpCode.HighWord += 1;
                                //line.Body.Insert(1, new EVEOpCode(0x0));
                                //line.LineLengthOpCode.HighWord += 1;
                                //line.Body.Insert(1, new EVEOpCode(0x0));
                                //line.LineLengthOpCode.HighWord += 1;

                                ///////////////////////////////////

                                var subtitleGevId = $"SBB{subtitleId:D2}";
                                var scnName = $"SUB{subtitleId:D2}";

                                mechSpawnLine.Body.InsertRange(
                                    //referencingLine.Body.Count - 1 - 3,
                                    //6,
                                    //1,
                                    mechSpawnLine.Body.Count - 2,
                                    [
                                    new EVEOpCode(0x0056, gev.GetOrInsertId(subtitleGevId)),
                                    new EVEOpCode(subtitlesObjectBytes.Take(4)), //first 4 chars of random weapon code
                                    new EVEOpCode(subtitlesObjectBytes.Skip(4)), //second 4 chars of random weapon code
                                    new EVEOpCode(0x82C882B5), // なし (none) as attachment/position
                                    new EVEOpCode(0x00000000), //unused 4 chars of attachment string
                                    ]);
                                //object load/spawn
                                mechSpawnLine.LineLengthOpCode.HighWord += 5;

                                mechSpawnLine.Body.InsertRange(
                                    //referencingLine.Body.Count - 1 - 3,
                                    //6,
                                    //1,
                                    mechSpawnLine.Body.Count - 2,
                                    [
                                    //TODO do something clever with spawn position, on AA01 it spawns off-map
                                    //TODO but on ME01 and AA02 it spawns floating in middle of map
                                    //TODO find, or create, Point thats neatly off-map?
                                    //TODO maybe short-hand PilotParam is an issue?

                                    new EVEOpCode(0x00010000), //0001000(1/0) is ally/enemy? 
                                    new EVEOpCode(0x0001, gev.GetOrInsertId(subtitleGevId)), //unused 4 chars of attachment string
                                    new EVEOpCode(0x00020000), //unused 4 chars of attachment string
                                    ]);
                                //position
                                mechSpawnLine.LineLengthOpCode.HighWord += 3;

                                mechSpawnLine.Body.InsertRange(
                                    //referencingLine.Body.Count - 1 - 3,
                                    //6,
                                    //1,
                                    mechSpawnLine.Body.Count - 2,
                                    [
                                    //crashes with ofs->evexxx but works with ofs->Boss0?
                                    new EVEOpCode(0x006A, gev.GetOrInsertId(subtitleGevId)), //pilot param bound to voice file name
                                    new EVEOpCode(sbytes.Take(4)), //first 4 chars of pilot param code
                                    new EVEOpCode(sbytes.Skip(4)), //second 4 chars of pilot param code
                                    new EVEOpCode(0x82C882B5), // なし (none) as attachment/position
                                    new EVEOpCode(0x00000000), //unused 4 chars of attachment string
                                    ]);
                                //pilot param ref
                                mechSpawnLine.LineLengthOpCode.HighWord += 5;

                                ///////////////////////////////////////


                                mechSpawnLine.Body.InsertRange(
                                    //1,
                                    mechSpawnLine.Body.Count - 2,
                                    [
                                    new EVEOpCode(0x00FA, gev.GetOrInsertId(subtitleGevId)), //TODO load Pilot Param and pass its id here
                                    //new EVEOpCode(gev.GetOrInsertId($"Other{subtitleId}"), 0xFFFF) //eva*** - to make ImgCutIn and Voice match in EvcScene
                                    new EVEOpCode(gev.GetOrInsertId(scnName), 0xFFFF) //eva*** - to make ImgCutIn and Voice match in EvcScene
                                    ]);
                                mechSpawnLine.LineLengthOpCode.HighWord += 2;

                                Console.WriteLine($"Prefetching ImgCutIn for voice file '{voiceFile}'");
                                sbytes = voiceFile.ToBytes(Encoding.ASCII, fixedLength: 8);

                                if (true && prefetchedImgCutIns.Add(voiceFile))
                                {
                                    prefetchLine.Body.InsertRange(
                                        prefetchLine.Body.Count - 1
                                        , [
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
                                else
                                {

                                }

                                var evcName = $"Sub{subtitleId:D2}";

                                Console.WriteLine($"ScnName (GEV): {scnName}");
                                Console.WriteLine($"Evcname (EVC): {evcName}");

                                var unitNode = new XElement("Unit");
                                //TODO generate GEV actor
                                unitNode.Add(new XElement("ScnName", scnName));

                                unitNode.Add(new XElement("EvcName", evcName));

                                var evcCutUnit = cutEvcUnitXml.XPathSelectElement($".//Name[text() = '{evcName}']");
                                if (evcCutUnit is null)
                                {
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
                                //break;
                            }

                            (cutAnimArc["/arc/EvcCut.xbf"] as U8FileNode).File = new XBFFile(cutEvcUnitXml.Document);

                            File.WriteAllText(cutEvcTxtPath.Replace(".txt", ".mod.txt"), cutEvcUnitDocument.ToString());

                            //break;
                        }

                        if (processedEVC.Add(evcPath))
                        {
                            var moddumpXmplPath = evcPath.Replace("_clean", "_dirty") + ".mod.txt";

                            File.WriteAllText(moddumpXmplPath, xml.ToString());

                            //xml = XDocument.Load(evcPath.Replace("_clean", "_dirty") + ".txt");
                            (evcScene as U8FileNode).File = new XBFFile(xml);
                            var bbbb = new ByteBuffer();
                            mU8.Serialize(evcArc, bbbb, ctx, out _);
                            File.WriteAllBytes(evcPath.Replace("_clean", "_dirty"), bbbb.GetData());

                            //File.WriteAllText(evcPath.Replace("_clean", "_dirty") + ".txt", xml.ToString());
                        }
                    }

                    if (voicePlaybacks.Any())
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Updating Voice Playback line 0x{line.LineId:X4} #{line.LineId}");
                    }
                    foreach (var voicePlayback in voicePlaybacks)
                    {
                        var index = voicePlayback.Pos;
                        Console.WriteLine($"EVE {line.Body[voicePlayback.Pos]} {voicePlayback.Str}");

                        var avatarIndex = line.ParsedCommands.IndexOf(voicePlayback) + 1;
                        if (avatarIndex < parsedCommands.Count && parsedCommands[avatarIndex] is AvatarDisplay avatar)
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
                            if (processedCutIns.Add(voicePlayback.Str) && generateImgCutIn)
                                gen.RepackSubtitleTemplate(voicePlayback.Str, @"C:\G\Wii\R79JAF_dirty\DATA\files\_2d\ImageCutIn", pilotCodeOverride);

                            Console.WriteLine($"Updating Avatar display for {voicePlayback.Str} with {pilotCodeOverride}");

                            //update avatar.Str to match voice.Str
                            line.Body[avatar.Pos + 1] = new EVEOpCode(sbytes.Take(4));
                            line.Body[avatar.Pos + 2] = new EVEOpCode(sbytes.Skip(4).Take(4));
                            //display ImgCutIn instead of MsgBox
                            line.Body[avatar.Pos + 3] = new EVEOpCode(0x0002FFFF);


                            if (true && prefetchedImgCutIns.Add(voicePlayback.Str))
                            {
                                sbytes = voicePlayback.Str.ToBytes(Encoding.ASCII, fixedLength: 8);
                                Console.WriteLine($"Prefetching ImgCutIn for '{voicePlayback.Str}'");

                                prefetchLine.Body.InsertRange(
                                    prefetchLine.Body.Count - 1
                                    , [
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
                            else
                            {

                            }
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

            File.WriteAllText(@"C:\G\Wii\R79JAF_dirty\DATA\files\boot\boot.arc_Pilot_Param.xbf.txt", pilotParamXml.ToString());

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
