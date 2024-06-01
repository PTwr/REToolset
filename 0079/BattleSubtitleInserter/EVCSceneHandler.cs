using BinaryFile.Formats.Nintendo;
using BinaryFile.Formats.Nintendo.R79JAF;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BattleSubtitleInserter
{
    public class EVCSceneHandler
    {
        private readonly XDocument xml;
        private readonly U8FileNode? fileNode;
        public U8File U8File => fileNode.U8File;

        public void Save()
        {
            Cuts.ForEach(x => x.SaveNestedCut());
            fileNode.File = new XBFFile(xml);
        }

        public void ReplaceVoice(string original, string replacement)
        {
            foreach(var voice in xml.XPathSelectElements($"//Voice[text() = '{original}']"))
            {
                voice.Value = replacement;
            }
        }

        public List<EVCCutHandler> Cuts { get; set; } = [];
        public EVCSceneHandler(U8File evcArc)
        {
            var evcScene = evcArc["/arc/EvcScene.xbf"];
            fileNode = evcScene as U8FileNode;
            var xbf = (evcScene as U8FileNode).File as XBFFile;

            xml = xbf.ToXDocument();


            //each Cut has separate Frame times and Unit list
            foreach (var cut in xml.XPathSelectElements("//Cut"))
            {
                var c = new EVCCutHandler(cut, evcArc["/arc/" + cut.XPathSelectElement("./File").Value] as U8FileNode);

                //dont bother with silent scenes, or ones with only indirect voicelines
                if (c.Voices.Any())
                {
                    Cuts.Add(c);
                }
            }
        }

        public void RemoveImgCutIns()
        {
            foreach (var c in Cuts)
                c.RemoveImgCutIns();
        }
        public void RemoveFrameWaits()
        {
            foreach (var c in Cuts)
                c.RemoveFrameWaits();
        }

        public IEnumerable<string> VoiceFilesInUse() => Cuts.SelectMany(i => i.Voices).Select(i => i.VoiceName).Distinct();
    }
}
