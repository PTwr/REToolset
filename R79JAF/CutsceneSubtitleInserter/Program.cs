using BinaryDataHelper;
using BinaryFile.Formats.Nintendo;
using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;
using System.Xml.XPath;

namespace CutsceneSubtitleInserter
{
    internal class Program
    {
        static void __Main(string[] args)
        {
            var tutorialEVC = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\evc", "EVC_TU_*.arc");
            var aceEVC = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\evc", "EVC_AC_*.arc");
            var storyEVC = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\evc", "EVC_ST_*.arc");

            var allEvc = aceEVC.Concat(tutorialEVC).Concat(storyEVC);

            var ctx = PrepXBFMarshaling(out var mXBF, out var mU8, out var mGEV);

            foreach (var evcFile in allEvc)
            {
                var targetFile = evcFile.Replace("clean", "dirty");

                var u8 = mU8.Deserialize(null, null, File.ReadAllBytes(evcFile).AsMemory(), ctx, out _);

                var evcScene = (u8["/arc/EvcScene.xbf"] as U8FileNode).File as XBFFile;
                var str = evcScene.ToString();

                var xml = evcScene.ToXDocument();

                var cutins = xml.XPathSelectElements("//ImgCutIn");
                cutins.Remove();

                xml.XPathSelectElements("//Frame").Remove();

                foreach (var cut in xml.XPathSelectElements("//Cut"))
                {
                    var firstUnitName = cut.XPathSelectElement(".//EvcName")?.Value;
                    firstUnitName = "Unit0";
                    var unitId = 0;
                    var voices = cut.XPathSelectElements("./Voice[text() != 'End']");
                    int frame = 50;
                    foreach (var voice in voices)
                    {
                        var voiceFile = voice.Value;

                        var voiceWait = voice.PreviousNode as XElement;
                        //voiceWait.Value = "0";

                        var imgcutin = new XElement("ImgCutIn", firstUnitName + unitId.ToString());
                        unitId++;

                        //EVC_ST_1 (story? standard?) has Voice without pilot prefix and defines VoiceUnit
                        //dynamic voices for modes with selectable pilots?
                        if (voiceWait.Name != "VoiceWait")
                        {
                            continue;
                        }

                        voiceWait.AddBeforeSelf(imgcutin);

                        //YEEY this seems to work :)
                        var waitSum = voice.XPathSelectElements("preceding-sibling::VoiceWait")
                            .Select(i => float.Parse(i.Value))
                            .Sum();

                        var voiceSum = voice.XPathSelectElements("preceding-sibling::Voice")
                            .Select(i => GetBRSTMduration($@"C:\G\Wii\R79JAF_clean\DATA\files\sound\stream\{i.Value}.brstm"))
                            .Sum() * 60;

                        var delay = waitSum + voiceSum;
                        delay = Math.Ceiling(delay);

                        var frameNode = new XElement("Frame", delay.ToString());
                        frameNode.Add(new XAttribute("type", "f32"));
                        imgcutin.AddBeforeSelf(frameNode);

                        frame += 120;



                        //TODO recalculate voice file name to Pilot_Param name
                        //TODO then generate Pilot_Param->Voice mapping
                        //TODO aaand then update "actor" list in GEV

                    }
                }

                (u8["/arc/EvcScene.xbf"] as U8FileNode).File = new XBFFile(xml);


                var bb = new ByteBuffer();
                mU8.Serialize(u8, bb, ctx, out _);
                File.WriteAllBytes(targetFile, bb.GetData());

            }
        }
        static double GetBRSTMduration(string path)
        {
            var args = $"-i \"{path}\" " +
                "-show_entries format=duration " +
                "-v quiet " +
                "-of csv=\"p=0\" ";

            //ffprobe -i <file> -show_entries format=duration -v quiet -of csv="p=0"
            ProcessStartInfo psi = new ProcessStartInfo(@"ffprobe", args);
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            var p = Process.Start(psi);


            string line = p.StandardOutput.ReadLine();

            double duration = double.Parse(line, CultureInfo.InvariantCulture);

            p.WaitForExit();

            return duration;
        }
        static void Main(string[] args)
        {
            var tutorialEVC = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\evc", "EVC_TU_*.arc");
            var aceEVC = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\evc", "EVC_AC_*.arc");
            var storyEVC = Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\evc", "EVC_ST_*.arc");

            var ctx = PrepXBFMarshaling(out var mXBF, out var mU8, out var mGEV);

            var amuroAce01gev = mGEV.Deserialize(null, null,
                File.ReadAllBytes(@"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\ace\AA01.gev"),
                ctx, out _);

            var bootU8 = mU8.Deserialize(null, null, File.ReadAllBytes(@"C:\G\Wii\R79JAF_clean/DATA\files\boot\boot.arc").AsMemory(), ctx, out _);
            var mutliCode = (bootU8["/arc/pilot_param.xbf"] as U8FileNode).File as XBFFile;
            var mc = mutliCode.ToString();

            var xx = mutliCode.ToXDocument();



            var ss = xx.ToString();
            File.WriteAllText("c:/dev/tmp/pp.txt", ss);
            ss = File.ReadAllText("c:/dev/tmp/pp.txt");

            xx = XDocument.Parse(ss);


            //var newPilotVoiceGroup = new XElement("VOICE", "eve");
            //newPilotVoiceGroup.Add(new XAttribute("type", "string"));

            //var newPilot = new XElement("PILOT", newPilotVoiceGroup);
            //newPilot.Add(new XAttribute("type", "FORMAT"));
            //newPilot.Add(new XAttribute("name", "HM_EVE"));


            //xx.Root.Add(newPilot);

            //xx.Root.Elements().Last().Remove();

            //var pilotNumber = new XElement("Number", 28);
            //pilotNumber.Add(new XAttribute("type", "s32"));
            //var pilotCode = new XElement("PILOT_CODE", pilotNumber);
            //pilotCode.Add(new XAttribute("type", "FORMAT"));
            //pilotCode.Add(new XAttribute("name", "HM_TRD"));
            //xx.Root.Add(pilotCode);

            (bootU8["/arc/pilot_param.xbf"] as U8FileNode).File = new XBFFile(xx);

            (bootU8["/arc/pilot_param.xbf"] as U8FileNode).File
                = new XBFFile(XDocument.Parse(File.ReadAllText(@"C:\dev\tmp\BootArc\pilot_param.xbf.txt")));
            (bootU8["/arc/MultiCode.xbf"] as U8FileNode).File
                = new XBFFile(XDocument.Parse(File.ReadAllText(@"C:\dev\tmp\BootArc\MultiCode.xbf.txt")));

            var b = new ByteBuffer();
            mU8.Serialize(bootU8, b, ctx, out _);
            //TODO test read-write loop in files in boot.arc, some fuckery with encoding
            //some XBF are UTF8, some are ShiftJis, and some have chars that break anyway :/
            File.WriteAllBytes(@"C:\G\Wii\R79JAF_dirty\DATA\files\boot\boot.arc", b.GetData());

            return;
            var jumptable = amuroAce01gev.EVESegment.Blocks[0].EVELines[0].ToString();

            foreach (var file in aceEVC)
            {
                var u8 = mU8.Deserialize(null, null, File.ReadAllBytes(file).AsMemory(), ctx, out _);

                var evcScene = (u8["/arc/EvcScene.xbf"] as U8FileNode).File as XBFFile;
                var str = evcScene.ToString();
                File.WriteAllText(@$"C:\dev\tmp\EvcScene\{Path.GetFileName(file)}.xbf.txt", str);
                var xml = evcScene.ToXDocument();

                xml.Descendants("ImgCutIn").First().Value = "Unit02";
                //xml.Descendants("ImgCutIn").First().Value = "_2d/ImageCutIn/IC_KAI.arc";

                (u8["/arc/EvcScene.xbf"] as U8FileNode).File = new XBFFile(xml);


                var nestedArcs = (u8["/arc"] as U8DirectoryNode)
                    .Children.Where(i => i.Name.EndsWith(".arc"))
                    .OfType<U8FileNode>();

                foreach (var nestedArc in nestedArcs)
                {
                    var xbfNode = (nestedArc.File as U8File)["/arc/EvcCut.xbf"] as U8FileNode;
                    var xbf = xbfNode.File as XBFFile;
                    var str2 = xbf.ToString();
                }

                var bb = new ByteBuffer();
                mU8.Serialize(u8, bb, ctx, out _);
                File.WriteAllBytes(file.Replace("clean", "dirty"), bb.GetData());
            }

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
