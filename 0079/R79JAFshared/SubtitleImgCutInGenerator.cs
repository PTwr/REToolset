using BinaryDataHelper;
using BinaryFile.Formats.Nintendo;
using BinaryFile.Marshaling.Common;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

namespace R79JAFshared
{
    public class SubtitleImgCutInGenerator
    {
        public const int RenderWidth = 683;
        public const int RenderHeight = 128;
        private readonly string subtitleAssetsDirectory;
        private readonly string brstmDirectory;
        private readonly string textDirectory;
        private readonly string mtlTextDirectory;
        private readonly string tempDir;
        private readonly string cutinTargetDir;
        private readonly IMarshalingContext marshalingCtx;
        private readonly ITypeMarshaler<U8File> mU8;

        public SubtitleImgCutInGenerator(string subtitleAssetsDirectory, string brstmDirectory, string textDirectory, string tempDir, string cutingTargetDir, string mtlTextDirectory)
        {
            this.subtitleAssetsDirectory = subtitleAssetsDirectory;
            this.brstmDirectory = brstmDirectory;
            this.textDirectory = textDirectory;
            this.tempDir = tempDir;
            this.cutinTargetDir = cutingTargetDir;
            marshalingCtx = PrepU8Marshaling(out mU8);

            //WSZST has issues with leftover files
            if (Directory.Exists(this.tempDir))
            {
                Directory.Delete(this.tempDir, true);
            }
            Directory.CreateDirectory(this.tempDir);
            this.mtlTextDirectory = mtlTextDirectory;
        }

        private IMarshalingContext PrepU8Marshaling(out ITypeMarshaler<U8File> mU8)
        {
            var store = new DefaultMarshalerStore();
            var rootCtx = new RootMarshalingContext(store);

            U8Marshaling.Register(store);

            mU8 = store.FindMarshaler<U8File>();

            return rootCtx;
        }

        public class SubEntry
        {
            public string VoiceFile { get; set; }
            public int DisplayFrom { get; set; }
            public string PilotCodeOverride { get; set; }
        }
        public void RepackMultiVoiceSubtitleTemplate(List<SubEntry> subEntries, string code)
        {
            var templateBrres = subtitleAssetsDirectory + $"/MutliTemplates/{subEntries.Count:D2}.brres";
            var templateArc = subtitleAssetsDirectory + "/SubtitlesImgCutinTemplate.arc";

            var brresDir = tempDir + "/" + code;
            UnpacktemplateBrres(templateBrres, brresDir);

            for(int n = 0; n<subEntries.Count; n++) 
            {
                var sub = subEntries[n];

                var pilotCode = sub.PilotCodeOverride ?? GetActorFromVoice(sub.VoiceFile);

                if (pilotCode is null) throw new Exception("Pilot code is requried for avatar!");

                var pngPath = tempDir + "/" + sub.VoiceFile + ".png";
                var texPath = brresDir + $"/Textures(NW4R)/{n:D2}";

                UsedVoiceFiles.Add(sub.VoiceFile);
                string tlPath = textDirectory + "/" + sub.VoiceFile + ".brstm.txt";
                string mtlPath = mtlTextDirectory + "/" + sub.VoiceFile + ".brstm.txt";

                bool mtlFallback = true;

                string text = "";
                if (File.Exists(tlPath))
                {
                    text = File.ReadAllText(tlPath).Trim();
                    mtlFallback = string.IsNullOrWhiteSpace(text);
                }

                if (mtlFallback)
                {
                    text = File.ReadAllText(mtlPath).Trim();
                }

                if (EnableDebugToolTip)
                {
                    text = $"!!! File: '{sub.VoiceFile}' !!!" + (File.Exists(tlPath) ? "" : " MTL Placeholder!!!") + Environment.NewLine + text;
                }
                else if (MtlWarning)
                {
                    text = (File.Exists(tlPath) ? "" : $"MTL Placeholder!!!{Environment.NewLine}") + text;
                }

                if (pilotCode.Contains("Unknown"))
                    FacelessVoiceFiles.Add(sub.VoiceFile + " " + pilotCode);

                PrepareSubtitleImage(pilotCode, text, pngPath);
                ConvertToWiiTexture(pngPath, texPath);

                CorrectTextureVersionFrom3To1(texPath);

                var clrPath = brresDir + "/AnmClr(NW4R)/IMAGE_CUT_IN_00";

                var frameCount = DurationSecondsToFrameCount(
                    GetBRSTMduration(brstmDirectory + "/" + sub.VoiceFile + ".brstm")
                    );
                CLR0Patcher.PatchClr0(clrPath, sub.DisplayFrom, sub.DisplayFrom + frameCount);
            }

            var newBressFile = tempDir
                + "/IC_" + code
                + ".brres";
            PackSubtitleBrres(brresDir, newBressFile);

            PackSubtitleArc(cutinTargetDir, templateArc, newBressFile);
        }

        public HashSet<string> UsedVoiceFiles = new HashSet<string>();
        public HashSet<string> FacelessVoiceFiles = new HashSet<string>();
        public bool EnableDebugToolTip = false;
        public bool MtlWarning = true;
        public bool RepackSubtitleTemplate(string voice, string pilotCodeOverride = null)
        {
            UsedVoiceFiles.Add(voice);

            var pilotCode = pilotCodeOverride ?? GetActorFromVoice(voice);
            if (pilotCode is null)
                return false;
            //throw new Exception($"Invalid voice: {voice}");

            string tlPath = textDirectory + "/" + voice + ".brstm.txt";
            string mtlPath = mtlTextDirectory + "/" + voice + ".brstm.txt";

            bool mtlFallback = true;

            string text = "";
            if (File.Exists(tlPath))
            {
                text = File.ReadAllText(tlPath).Trim();
                mtlFallback = string.IsNullOrWhiteSpace(text);
            }

            if (mtlFallback)
            {
                text = File.ReadAllText(mtlPath).Trim();
            }

            if (EnableDebugToolTip)
            {
                text = $"!!! File: '{voice}' !!!" + (File.Exists(tlPath) ? "" : " MTL Placeholder!!!") + Environment.NewLine + text;
            }
            else if (MtlWarning)
            {
                text = (File.Exists(tlPath) ? "" : $"MTL Placeholder!!!{Environment.NewLine}") + text;
            }

            var frameCount = DurationSecondsToFrameCount(
                GetBRSTMduration(brstmDirectory + "/" + voice + ".brstm")
                );

            var templateBrres = subtitleAssetsDirectory + "/SubtitlesImgCutinTemplate.brres";
            var templateArc = subtitleAssetsDirectory + "/SubtitlesImgCutinTemplate.arc";

            var brresDir = tempDir + "/" + voice;
            UnpacktemplateBrres(templateBrres, brresDir);

            var pngPath = tempDir + "/" + voice + ".png";
            var texPath = brresDir + "/Textures(NW4R)/ImageCutIn_00";

            if (pilotCode.Contains("Unknown"))
                FacelessVoiceFiles.Add(voice + " " + pilotCode);

            PrepareSubtitleImage(pilotCode, text, pngPath);
            ConvertToWiiTexture(pngPath, texPath);

            CorrectTextureVersionFrom3To1(texPath);

            var clrPath = brresDir + "/AnmClr(NW4R)/IMAGE_CUT_IN_00";
            UpdateCLRduration(frameCount, clrPath);

            var newBressFile = tempDir
                + "/IC_" + voice
                + ".brres";
            PackSubtitleBrres(brresDir, newBressFile);

            PackSubtitleArc(cutinTargetDir, templateArc, newBressFile);

            return true;
        }

        private void PackSubtitleArc(string targetDir, string templateArc, string newBressFile)
        {
            var arc = mU8.Deserialize(null, null, File.ReadAllBytes(templateArc).AsMemory(), marshalingCtx, out _);

            (arc["/arc/IC_CHR.brres"] as U8FileNode).File =
                new RawBinaryFile()
                {
                    Data = File.ReadAllBytes(newBressFile)
                };

            var b = new ByteBuffer();
            mU8.Serialize(arc, b, marshalingCtx, out _);

            var targetArc = targetDir + "/" + Path.GetFileNameWithoutExtension(newBressFile) + ".arc.";
            File.WriteAllBytes(targetArc, b.GetData());
        }

        private static void UpdateCLRduration(ushort frameCount, string clrPath)
        {
            //load constant-color CLR0
            var clr0template = File.ReadAllBytes(clrPath);

            //update frame count to match calculated
            BinaryPrimitives.WriteUInt16BigEndian(clr0template.AsSpan(0x1C), frameCount);

            //and replace file extracted by Wiimm
            File.WriteAllBytes(clrPath, clr0template);
        }

        static void CorrectTextureVersionFrom3To1(string texPath)
        {
            //HACK Wiimm tools output tex0 v3, R79JAF is using v1 but its also not using any v3 features so just changing version number seems to work
            var texture = File.ReadAllBytes(texPath);
            texture[11] = 0x01;
            File.WriteAllBytes(texPath, texture);
        }

        void ConvertToWiiTexture(string png, string targetFilename)
        {
            ProcessStartInfo psi = new ProcessStartInfo(@"C:\Program Files\Wiimm\SZS\wimgt.exe",
                $"ENCODE \"{png}\" " +
                "--overwrite " +
                "--transform CMPR " +
                "--n-mipmaps 0 " + //mipmaps are breaking brres create, and --no-mipmaps does NOT disable them :D
                $"--DEST \"{targetFilename}\"");
            var p = Process.Start(psi);
            p.WaitForExit();
        }

        void UnpacktemplateBrres(string source, string targetDir)
        {
            ProcessStartInfo psi = new ProcessStartInfo(@"C:\Program Files\Wiimm\SZS\wszst.exe",
                $"EXTRACT \"{source}\" " +
                "--overwrite " +
                $"--DEST \"{targetDir}\"");
            var p = Process.Start(psi);
            p.WaitForExit();
        }
        void PackSubtitleBrres(string sourceDir, string targetFile)
        {
            ProcessStartInfo psi = new ProcessStartInfo(@"C:\Program Files\Wiimm\SZS\wszst.exe",
                $"CREATE \"{sourceDir}\" " +
                "--overwrite " +
                $"--DEST \"{targetFile}\"");
            var p = Process.Start(psi);
            p.WaitForExit();
        }

        public string GetActorFromVoice(string voice)
        {
            var group = new string(voice.Where(i => char.IsLetter(i)).Take(3).ToArray());

            //dont do it for music
            if (group is "bgm") return null;
            //nor narrative
            if (group is "na") return null;
            //or tutorial
            //if (group is "tut") return;
            //or exam robo shouts
            if (group is "exm") return null;
            //or mobile suit variation descriptions :)
            if (group is "msv") return null;

            //TODO filter out intermission and chat

            //TODO gather ev*->face mapping from GEV msg window calls. EVC will still need manual input
            var avatar = group; //for pilots voice group matches avatar

            if (group is "eva") avatar = "Unknown Ace"; //ace
            if (group is "eve") avatar = "Unknown EFSF"; //eff
            if (group is "evs") avatar = "Unknown Shared"; //shared
            if (group is "evz") avatar = "Unknown Zeon"; //zeon

            if (group is "tut") avatar = "Unknown Tutorial"; //tutorial

            //random minion battle chatter
            //TODO get bunch of ugly mugs from anime? :D
            if (group is "sla") avatar = "Unknown Soldier";
            if (group is "slb") avatar = "Unknown Soldier";
            if (group is "slc") avatar = "Unknown Soldier";
            if (group is "sld") avatar = "Unknown Soldier";
            if (group is "sle") avatar = "Unknown Soldier";
            if (group is "slf") avatar = "Unknown Soldier";

            return avatar;
        }

        public double GetBRSTMduration(string path)
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

        public ushort DurationSecondsToFrameCount(double seconds, int frameRate = 60)
        {
            var SperF = 1.0 / frameRate;
            var frameCount = seconds / SperF;

            return (ushort)(frameCount);
        }

        public void PrepareSubtitleImage(string pilotCode, string text, string targetPath, int subtitleHeight = 128)
        {
            //correction for 512x512 forced into 4:3
            int avatarWidth = subtitleHeight;// (int)(subtitleHeight * 0.75);

            var bmp = new Bitmap(RenderWidth, RenderHeight);
            var g = Graphics.FromImage(bmp);

            g.FillRectangle(
                Brushes.Transparent, 0, 0, RenderWidth, RenderHeight
                );

            g.FillRectangle(Brushes.Black, 0, 0, RenderWidth, subtitleHeight);

            var avatarpath = $"{subtitleAssetsDirectory}/Avatars/{pilotCode}.png";
            if (File.Exists(avatarpath) is false) avatarpath = $"{subtitleAssetsDirectory}/Avatars/hu1.png";
            var avatar = Image.FromFile(avatarpath);
            g.DrawImage(avatar, 0, 0, avatarWidth, subtitleHeight);

            Font font1 = new Font("Arial", 22, FontStyle.Bold, GraphicsUnit.Point);

            RectangleF rectF1 = new RectangleF(avatarWidth, 0, RenderWidth - avatarWidth, subtitleHeight);

            StringFormat format = new StringFormat();
            format.LineAlignment = StringAlignment.Center;
            format.Alignment = StringAlignment.Center;

            var dim = g.MeasureString(text, font1, RenderWidth - avatarWidth, format);
            while (dim.Width > rectF1.Width || dim.Height > rectF1.Height)
            {
                font1 = new Font("Arial", font1.Size - 0.1F, FontStyle.Bold, GraphicsUnit.Point);
                dim = g.MeasureString(text, font1, RenderWidth - avatarWidth, format);
            }

            g.DrawString(text, font1, Brushes.White, rectF1, format);

            var resized = new Bitmap(bmp, new Size(512, 128));

            resized.Save(targetPath, ImageFormat.Png);
        }
    }
}
