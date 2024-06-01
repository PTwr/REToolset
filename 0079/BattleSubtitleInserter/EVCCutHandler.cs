using BinaryFile.Formats.Nintendo;
using BinaryFile.Formats.Nintendo.R79JAF;
using R79JAFshared;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BattleSubtitleInserter
{
    public class EVCCutHandler
    {
        private readonly XElement sceneCutElement;

        //to update Cut nested arc/xbf
        private readonly U8FileNode cutXbfFile;
        //to add Units to nested Cut file
        private readonly XElement? cutEvcUnitXml;

        public void SaveNestedCut()
        {
            cutXbfFile.File = new XBFFile(cutEvcUnitXml.Document);
        }

        public EVCCutHandler(XElement sceneCutElement, U8FileNode cutFile)
        {
            var cutAnimArc = cutFile.File as U8File;
            this.cutXbfFile = (cutAnimArc["/arc/EvcCut.xbf"] as U8FileNode);
            var cutAnimxbf = cutXbfFile.File as XBFFile;
            var cutEvcUnitDocument = cutAnimxbf.ToXDocument();

            cutEvcUnitXml = cutEvcUnitDocument.XPathSelectElement("//EvcUnit");
            if (cutEvcUnitXml is null)
            {
                cutEvcUnitXml = new XElement("EvcUnit");
                cutEvcUnitDocument.Root.Add(cutEvcUnitXml);
            }

            this.sceneCutElement = sceneCutElement;
        }

        public void AddUnit(string gevName, string evcName)
        {
            //add Unit to Scenario Cut node
            var sceneUnit = sceneCutElement.XPathSelectElement($".//EvcName[text() = '{evcName}']");
            if (sceneUnit is null)
            {
                sceneCutElement.Add(new XElement("Unit", 
                    new XElement("ScnName", gevName),
                    new XElement("EvcName", evcName)
                    ));
            }

            //add Unit to nested (animation) Cut file
            var evcCutUnit = cutEvcUnitXml.XPathSelectElement($".//Name[text() = '{evcName}']");
            if (evcCutUnit is null)
            {
                cutEvcUnitXml.Add(new XElement("Unit", new XElement("Name", evcName)));
            }
        }

        public void AddImgCutIn(string evcName, int frameDelay)
        {
            //TODO AddBefore Voice? It might not matter, but ImgCutIn+Frame should come in pairs? Or are they two paired lists and only order in their category matters?
            var imgcutin = new XElement("ImgCutIn", evcName);
            sceneCutElement.Add(imgcutin);

            var frameNode = new XElement("Frame", frameDelay.ToString());
            frameNode.Add(new XAttribute("type", "f32"));
            imgcutin.AddBeforeSelf(frameNode);
        }

        public IEnumerable<EvcVoiceInfo> Voices => sceneCutElement
            .XPathSelectElements("./Voice[text() != 'End']")
            .Select(voice => new
            {
                Node = voice,
                PreceedingVoicesWait = voice.XPathSelectElements("preceding-sibling::VoiceWait")
                                    .Select(i => float.Parse(i.Value))
                                    .Sum(),
                PreceedingVoicesDuration = voice.XPathSelectElements("preceding-sibling::Voice")
                                    .Select(i => i.Value)
                                    .Select(i => ExternalToolsHelper.GetBRSTMduration(Env.VoiceFileAbsolutePath(voice.Value)))
                                    .Sum() * 60,
            })
            .Select(data => new EvcVoiceInfo(
                data.Node.Value,
                (int)Math.Ceiling(data.PreceedingVoicesWait + data.PreceedingVoicesWait),
                (int)Math.Ceiling(ExternalToolsHelper.GetBRSTMduration(Env.VoiceFileAbsolutePath(data.Node.Value)))
            ));

        public void RemoveImgCutIns()
        {
            //remove all existing CutIn calls
            sceneCutElement.XPathSelectElements(".//ImgCutIn").Remove();
        }
        public void RemoveFrameWaits()
        {
            //and frame waits which hopefully are only for CutIns
            sceneCutElement.XPathSelectElements(".//Frame").Remove();
        }
    }
}
