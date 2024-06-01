using BinaryFile.Formats.Nintendo;
using BinaryFile.Formats.Nintendo.R79JAF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BattleSubtitleInserter
{
    public class PilotParamHandler
    {
        private readonly U8FileNode fileNode;
        private XDocument xml;
        public U8File U8File => fileNode.U8File;

        public PilotParamHandler(U8File file) : this(((U8FileNode)file["/arc/pilot_param.xbf"]))
        { }
        public PilotParamHandler(U8FileNode fileNode)
        {
            this.fileNode = fileNode;
            var xbf = fileNode.File as XBFFile;
            xml = xbf!.ToXDocument();
        }

        public void Save() => fileNode.File = new XBFFile(xml);

        public void AddPilotParam(string code, string imgcutin, bool includeAllFields = false)
        {
            //do not insert duplicates
            if (xml.XPathSelectElement($"//PILOT[@name = '{code}']") is not null)
                return;

            //all values are not needed when not spawning mech for placeholder AI
            if (includeAllFields)
            {
                var existingPilotParam =
                    xml.XPathSelectElement("//PILOT[@name = 'HM_DEFAULT']")!;

                //deep clone
                var pilotNode = new XElement(existingPilotParam);
                //and update important fields
                pilotNode.XPathSelectElement(".//VOICE")!.Value = imgcutin;
                pilotNode.Attribute("name")!.Value = code;
            }
            else
            {
                var pilotNode = new XElement("PILOT");
                pilotNode.Add(new XAttribute("type", "FORMAT"));
                pilotNode.Add(new XAttribute("name", code));
                var voiceNode = new XElement("VOICE", imgcutin);
                voiceNode.Add(new XAttribute("type", "string"));
                pilotNode.Add(voiceNode);

                xml.Root!.Add(pilotNode);
            }
        }

        public static string VoiceFileToPilotPram(string voiceFile)
        {
            //unused prefix
            var result = "Z";

            //There is limit of 7 chars? 8th char has to be null?
            if (voiceFile.EndsWith('b'))
            {
                //voiceFile = "eve999";
                voiceFile = voiceFile.TrimEnd('b');
                //change prefix to avoid conflict
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


            return result;
        }
    }
}
