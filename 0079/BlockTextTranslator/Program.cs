using BinaryDataHelper;
using BinaryFile.Formats.Nintendo;
using BinaryFile.Formats.Nintendo.R79JAF;
using Newtonsoft.Json;
using R79JAFshared;
using System.Diagnostics;
using System.Xml.Linq;
using System.Xml.XPath;
using TranslationHelpers;

namespace BlockTextTranslator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var ctx = MarshalingHelper.PrepXBFMarshaling(out var mXBF, out var mU8, out var mGEV);

            HierarchicalDictionary<string, BlockTextEntry> globalDict = new HierarchicalDictionary<string, BlockTextEntry>();

            var dictRootDir = @"C:\Users\PTwr\Documents\GitHub\0079tl\Patcher\Patch\Unique";
            FillDictFromDir(globalDict, dictRootDir);

            var bb = new ByteBuffer();

            TraverseDirectory(globalDict, Env.CleanCopyFilesDirectory, "*.arc",
                (dict, file) =>
                {
                    //TODO missionvoice.arc has gevs with no STR sections and empty OFS, current map does not handle it
                    if (!file.Contains("_2d")) return;

                    var u8 = mU8.Deserialize(null, null, File.ReadAllBytes(file).AsMemory(), ctx, out _);

                    var trav = u8 as ITraversable;

                    var c = trav.DescendantsOfType<XBFFile>().ToList();

                    bool modified = false;

                    trav.TraverseOfType<XBFFile>(blockTextXbf =>
                    {
                        if (blockTextXbf?.Parent?.Name != "BlockText.xbf") return;

                        var stringGroupXbf = (blockTextXbf.Parent.ParentNode.Children
                            .Where(i => i.Name == "StringGroup.xbf")
                            .OfType<U8FileNode>()
                            .FirstOrDefault()
                            .File as XBFFile);

                        var stringGroupXml = stringGroupXbf.ToXDocument();

                        var blockTextXml = blockTextXbf.ToXDocument();

                        //TODO special handling for Briefing Chat, automatic line split

                        var nestedArcPathSegments = blockTextXbf.Parent.NestedPath.Split("/", StringSplitOptions.RemoveEmptyEntries);

                        var dir = dictRootDir + "/" + Path.GetRelativePath(Env.CleanCopyFilesDirectory, file);

                        //dont forget to check directory named after arc file itself
                        nestedArcPathSegments = ["", .. nestedArcPathSegments];

                        foreach (var segment in nestedArcPathSegments)
                        {
                            dir += "/" + segment;

                            var d = new HierarchicalDictionary<string, BlockTextEntry>(dict);

                            FillDictFromDir(d, dir);

                            if (d.Any())
                                dict = d;
                        }


                        modified |= UpdateBlockText(blockTextXml, stringGroupXml, dict);

                        blockTextXbf.Parent.File = new XBFFile(blockTextXml);
                        stringGroupXbf.Parent.File = new XBFFile(stringGroupXml);

                        Console.WriteLine(file);
                        Console.WriteLine($"Modified: {modified}");
                    });

                    if (modified)
                    {
                        bb.ResizeToAtLeast(0);

                        mU8.Serialize(u8, bb, ctx, out _);

                        var outputPath = Env.FromCleanToDirty(file);

                        File.WriteAllBytes(outputPath, bb.GetData());
                    }
                },
                (state, dir) =>
                {
                    var dict = new HierarchicalDictionary<string, BlockTextEntry>(state);

                    var nestedDictPath = Path.GetRelativePath(Env.CleanCopyFilesDirectory, dir);
                    nestedDictPath = dictRootDir + "/" + nestedDictPath;

                    FillDictFromDir(dict, nestedDictPath);

                    return dict.Any() ? dict : state;
                });
        }

        private static bool UpdateBlockText(XDocument blockText, XDocument stringGroup, HierarchicalDictionary<string, BlockTextEntry> dict)
        {
            bool modified = false;
            foreach (var block in blockText.XPathSelectElements(".//Block"))
            {
                var id = block.XPathSelectElement(".//ID").Value;

                //if (id == "MISSION_SELECT")
                //{
                //    Debugger.Break();
                //}

                if (dict.TryGetValue(id, out var tl))
                {
                    modified = true;

                    var strGrp = stringGroup.XPathSelectElement($".//String[./Code = '{id}']");

                    var txt = block.XPathSelectElement(".//Text");
                    txt.Value = tl.ToString();

                    if (!string.IsNullOrWhiteSpace(tl.Code))
                    {
                        strGrp.XPathSelectElement("./Code").Value = tl.Code;
                    }
                    if (!string.IsNullOrWhiteSpace(tl.PositionFlag))
                    {
                        strGrp.XPathSelectElement("./PositionFlag").Value = tl.PositionFlag;
                    }
                    if (!string.IsNullOrWhiteSpace(tl.CharSpace))
                    {
                        strGrp.XPathSelectElement("./CharSpace").Value = tl.CharSpace;
                    }
                    if (!string.IsNullOrWhiteSpace(tl.LineSpace))
                    {
                        strGrp.XPathSelectElement("./LineSpace").Value = tl.LineSpace;
                    }
                    if (!string.IsNullOrWhiteSpace(tl.TabSpace))
                    {
                        strGrp.XPathSelectElement("./TabSpace").Value = tl.TabSpace;
                    }
                    if (!string.IsNullOrWhiteSpace(tl.Color))
                    {
                        strGrp.XPathSelectElement("./Color").Value = tl.Color;
                    }
                    if (!string.IsNullOrWhiteSpace(tl.Size))
                    {
                        strGrp.XPathSelectElement("./Size").Value = tl.Size;
                    }
                }
            }

            foreach (var x in dict.Values
                .Where(i => i.Value.Mode == BlockTextMode.Insert)
                .Select(i=>i.Value))
            {
                var block = new XElement("Block");
                blockText.XPathSelectElement("//Texts").Add(block);

                block.Add(new XElement("ID", x.ID));
                block.Add(new XElement("Text", x.ToString()));

                var str = new XElement("String");
                stringGroup.XPathSelectElement("//StringGroup").Add(str);

                str.Add(new XElement("Code",string.IsNullOrWhiteSpace(x.Code) ? x.ID : x.Code));
                str.Add(new XElement("PositionFlag", x.PositionFlag ?? "256"));
                str.Add(new XElement("CharSpace", x.CharSpace ?? "1"));
                str.Add(new XElement("LineSpace", x.LineSpace ?? "1"));
                str.Add(new XElement("TabSpace", x.TabSpace ?? "1"));
                str.Add(new XElement("Color", x.Color ?? "-1"));
                str.Add(new XElement("Size", x.Size ?? "1"));
                str.Add(new XElement("ID", x.ID));

                modified = true;
            }

            return modified;
        }

        private static void FillDictFromDir(HierarchicalDictionary<string, BlockTextEntry> dict, string dir, string lang = "en")
        {
            if (Directory.Exists(dir) is false) return;
            foreach (var dictpath in Directory.EnumerateFiles(dir, $"dict*.{lang}.json", SearchOption.TopDirectoryOnly))
            {
                var data = JsonConvert.DeserializeObject<List<BlockTextEntry>>(File.ReadAllText(dictpath));
                dict.AddRange(data.ToDictionary(i => i.ID, i => i));
            }
        }

        static void TraverseDirectory<TState>(TState state, string directory, string filter,
            Action<TState, string> handler,
            Func<TState, string, TState> nestedState)
        {
            var nested = Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly)
                .SelectMany(i => Directory.EnumerateFiles(i, filter, SearchOption.TopDirectoryOnly));

            foreach (var file in Directory.EnumerateFiles(directory, filter, SearchOption.TopDirectoryOnly))
            {
                handler(state, file);
            }

            foreach (var dir in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
            {
                var s = nestedState(state, dir);
                TraverseDirectory(s, dir, filter, handler, nestedState);
            }
        }
    }

    public enum BlockTextMode
    {
        Update = 0,
        Insert = 1,
    }
    public class BlockTextEntry
    {
        public BlockTextMode Mode { get; set; }

        [JsonIgnore]
        public string Source { get; set; }

        public double LineSplit { get; set; }
        public List<string> Split()
        {
            return Lines
                .Select((s, n) => (s, n))
                .GroupBy(x => (int)(x.Item2 / LineSplit)) //group into sequential chunks
                .Select(x => string.Join("\n", x.Select(y => y.s)))
                .ToList();
        }

        public string ID { get; set; }
        public string Code { get; set; }
        public string Text { get; set; }
        public string[] Lines { get; set; }

        public string? Color { get; set; }
        public string? TabSpace { get; set; }
        public string? Size { get; set; }

        public string? CharSpace { get; set; }
        public string? LineSpace { get; set; }
        public string? PositionFlag { get; set; }

        public override string ToString()
        {
            if (Lines?.Any() == true)
            {
                return string.Join("\n", Lines);
            }
            return Text;
        }
    }
}
