using BinaryDataHelper;
using BinaryFile.Formats.Nintendo;
using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Marshaling.Common;
using Newtonsoft.Json;
using R79JAFshared;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using TranslationHelpers;

namespace BlockTextTranslator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string language = "en";
            bool debugTooltip = false;

            //Env.DirtyCopyFilesDirectory = @"C:\g\Wii\R79JAF_Riivolution\R79JAF_EN_UI";

            var ctx = MarshalingHelper.PrepXBFMarshaling(out var mXBF, out var mU8, out var mGEV);

            HierarchicalDictionary<string, BlockTextEntry> globalDict = new HierarchicalDictionary<string, BlockTextEntry>();

            var dictRootDir = @"C:\Users\PTwr\Documents\GitHub\0079tl\Patcher\Patch\Unique";
            var luaRootDir = @"C:\Users\PTwr\Documents\GitHub\0079tl\Patcher\Patch\Common\Lua";
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
                        if (blockTextXbf?.Parent?.Name == "BlockText.xbf")
                        {
                            modified |= DefaultBlockTextUpdate(dict, file, blockTextXbf, dictRootDir, debugTooltip: debugTooltip);
                        }
                    });
                    trav.TraverseOfType<U8FileNode>(fileNode =>
                    {
                        if (Path.GetExtension(fileNode.Name) == ".lua")
                        {
                            //special handling for chat sections
                            if (fileNode.Name.StartsWith("CH_M"))
                            {
                                var dictFilename = $"dict-{(fileNode.Name.Contains("ME") ? "EFF" : "Zeon")}_{fileNode.Name.Substring(5, 2)}-chat.{language}.json";

                                var dictPath = Directory
                                    .EnumerateFiles(dictRootDir, dictFilename, SearchOption.AllDirectories)
                                    .FirstOrDefault();

                                if (dictPath is not null)
                                {
                                    Console.WriteLine($"Updating {fileNode.Name} to match {dictPath}");

                                    var data = JsonConvert.DeserializeObject<List<BlockTextEntry>>(File.ReadAllText(dictPath));

                                    var originalTxt = (fileNode.File as RawBinaryFile)
                                        .Data.AsSpan()
                                        .ToDecodedString(BinaryStringHelper.Shift_JIS);

                                    var lines = originalTxt.SplitLines();

                                    for (int i=0;i<lines.Length;i++)
                                    {
                                        var textIdLine = lines[i];
                                        if (textIdLine.TrimStart().StartsWith("text_id"))
                                        {
                                            var voiceFile = textIdLine.Split("\"")[1].Split("_")[0];

                                            var textBlocks = data.Where(i => i.ID.StartsWith(voiceFile)).ToList();
                                            var textIds = textBlocks
                                                .Select(x => $"\"{x.ID}\", ")
                                                .ToList();

                                            lines[i] = $"text_id = {{ {string.Join("", textIds)} }},";

                                            var voiceWaitLine = lines[i + 1];

                                            var expectedWaitCount = textIds.Count;

                                            var waits = voiceWaitLine
                                                .Split(new char[] { '{', '}' })[1]
                                                .Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                                            if (expectedWaitCount < waits.Length)
                                            {
                                                waits = waits.Take(expectedWaitCount).ToArray();
                                                waits[^1] = "-1";
                                            }
                                            else if (expectedWaitCount > waits.Length)
                                            {
                                                var missingWaits = expectedWaitCount - waits.Length;
                                                waits = waits.Concat(Enumerable.Repeat("-1", missingWaits)).ToArray();
                                            }

                                            for(int j=0;j< textBlocks.Count; j++)
                                            {
                                                if (!string.IsNullOrWhiteSpace(textBlocks[j].ChatWait))
                                                {
                                                    waits[j] = textBlocks[j].ChatWait;
                                                }
                                            }

                                            lines[i+1] = $"text_wait = {{ {string.Join(", ", waits)} }},";
                                        }
                                    }
                                    foreach(var textIdLine in lines.Where(s => s.TrimStart().StartsWith("text_id")))
                                    {
                                        var voiceFile = textIdLine.Split("\"")[1].Split("_")[0];
                                        Console.WriteLine($"Voice file: {voiceFile}");
                                    }

                                    var moddedLua = string.Join(Environment.NewLine, lines);

                                    (fileNode.File as RawBinaryFile).Data = moddedLua.ToBytes(BinaryStringHelper.Shift_JIS);

                                    modified |= true;
                                }
                            }
                            //default patcher/replacer
                            else
                            {
                                modified |= ReplaceLuaScript(fileNode, language, luaRootDir);
                                modified |= PatchLuaScriptLines(fileNode, language, luaRootDir);
                            }
                        }
                    });

                    if (modified)
                    {
                        bb.ResizeToAtLeast(0);

                        mU8.Serialize(u8, bb, ctx, out _);

                        var outputPath = Env.FromCleanToDirty(file);

                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
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

        private static bool PatchLuaScriptLines(U8FileNode fileNode, string language, string luaRootDir)
        {
            var linePatches = Directory
                .EnumerateFiles(luaRootDir, $"{Path.GetFileNameWithoutExtension(fileNode.Name)}.{language}.*.lua", SearchOption.AllDirectories);

            if (linePatches.Any())
            {
                var patchFiles = linePatches.Select(path => new
                {
                    text = File.ReadAllText(path),
                    line = int.Parse(path.Split(".")[^2])
                })
                //add from bottom to top, to simplify processing
                .OrderByDescending(i => i.line)
                .ToList();

                var originalTxt = (fileNode.File as RawBinaryFile)
                    .Data.AsSpan()
                    .ToDecodedString(BinaryStringHelper.Shift_JIS);

                var lines = originalTxt
                    .SplitLines()
                    .ToList();

                foreach (var replacement in patchFiles.Where(i => i.line < 0))
                {
                    var replacementLines = replacement.text
                        .SplitLines();

                    var start = Math.Abs(replacement.line) - 1;
                    for (int i = 0; i < replacementLines.Length; i++)
                    {
                        lines[start + i] = replacementLines[i];
                    }
                }

                foreach (var insert in patchFiles.Where(i => i.line > 0))
                {
                    lines.Insert(insert.line - 1, insert.text);
                }

                var moddedText = string.Join(Environment.NewLine, lines);
                (fileNode.File as RawBinaryFile).Data = moddedText.ToBytes(BinaryStringHelper.Shift_JIS);

                Console.WriteLine("Patching lines in LUA script: " + fileNode.NestedPath);

                return true;
            }

            return false;
        }

        private static bool ReplaceLuaScript(U8FileNode fileNode, string language, string luaRootDir)
        {
            var replacementLua = Directory
                .EnumerateFiles(luaRootDir, $"{Path.GetFileNameWithoutExtension(fileNode.Name)}.{language}.lua", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (File.Exists(replacementLua))
            {
                var data = File.ReadAllBytes(replacementLua);
                fileNode.File = new RawBinaryFile(data);

                Console.WriteLine("Replacing LUA script: " + fileNode.NestedPath);

                return true;
            }

            return false;
        }

        private static bool DefaultBlockTextUpdate(HierarchicalDictionary<string, BlockTextEntry> dict, string file, XBFFile blockTextXbf, string dictRootDir, bool debugTooltip = false)
        {
            var modified = false;

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


            modified |= UpdateBlockText(blockTextXml, stringGroupXml, dict, debugTooltip);

            blockTextXbf.Parent.File = new XBFFile(blockTextXml);
            stringGroupXbf.Parent.File = new XBFFile(stringGroupXml);

            if (modified)
            {
                Console.WriteLine(file);
                Console.WriteLine($"BlockText and StringGroup updated");
            }

            return modified;
        }

        private static bool UpdateBlockText(XDocument blockText, XDocument stringGroup, HierarchicalDictionary<string, BlockTextEntry> dict, bool debugTooltip = false)
        {
            bool modified = false;
            foreach (var block in blockText.XPathSelectElements(".//Block"))
            {
                var id = block.XPathSelectElement(".//ID").Value;
                var txt = block.XPathSelectElement(".//Text");

                if (dict.TryGetValue(id, out var tl))
                {
                    modified = true;

                    var strGrp = stringGroup.XPathSelectElement($".//String[./Code = '{id}']");

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

                if (debugTooltip)
                {
                    txt.Value = id;
                }
            }

            foreach (var x in dict.Values
                .Where(i => i.Value.Mode == BlockTextMode.Insert)
                .Select(i => i.Value))
            {
                var block = new XElement("Block");
                blockText.XPathSelectElement("//Texts").Add(block);

                block.Add(new XElement("ID", x.ID));
                block.Add(new XElement("Text", x.ToString()));

                var str = new XElement("String");
                stringGroup.XPathSelectElement("//StringGroup").Add(str);

                str.Add(new XElement("Code", string.IsNullOrWhiteSpace(x.Code) ? x.ID : x.Code));
                str.Add(new XElement("PositionFlag", x.PositionFlag ?? "256"));
                str.Add(new XElement("CharSpace", x.CharSpace ?? "1"));
                str.Add(new XElement("LineSpace", x.LineSpace ?? "1"));
                str.Add(new XElement("TabSpace", x.TabSpace ?? "1"));
                str.Add(new XElement("Color", x.Color ?? "-1"));
                str.Add(new XElement("Size", x.Size ?? "1"));
                str.Add(new XElement("ID", x.ID));

                modified = true;
            }

            foreach (var x in dict.Values
                .Where(i => i.Value.Mode == BlockTextMode.Delete)
                .Select(i => i.Value))
            {
                blockText.XPathSelectElement($".//Block[./ID = '{x.ID}']")?.Remove();
                stringGroup.XPathSelectElement($".//String[./Code = '{x.Code ?? x.ID}']")?.Remove();

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
        Delete = 2,
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

        public string? ChatWait { get; set; }

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
