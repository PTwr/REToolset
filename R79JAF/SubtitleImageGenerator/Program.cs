using BinaryDataHelper;
using BinaryFile.Formats.Nintendo;
using BinaryFile.Marshaling.Common;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using R79JAFshared;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;

namespace SubtitleImageGenerator
{
    internal class Program
    {
        [Obsolete("Rewrite to reusable form, GEV/EVC parsing will (try to) detect faces for EV* groups")]
        static void Main(string[] args)
        {
            var targetDir = @"C:\G\Wii\R79JAF patch assets\CutinSubtitleTextures";
            var translationRoot = @"C:\G\Wii\R79JAF patch assets\sound_translate\stream";
            var txtFiles = Directory.EnumerateFiles(translationRoot, "*.brstm.txt", SearchOption.AllDirectories);

            var gen = new SubtitleImgCutInGenerator(
                @"C:\G\Wii\R79JAF patch assets\SubtitleAssets",
                @"C:\G\Wii\R79JAF_dirty\DATA\files\sound\stream",
                @"C:\G\Wii\R79JAF patch assets\subtitleTranslation",
                @"C:\G\Wii\R79JAF patch assets\tempDir"
                );

            var ctx = PrepU8Marshaling(out var mU8);

            Parallel.ForEach(txtFiles, (txtFile) =>
            {
                //remove .brstm.txt
                var voiceFile = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(txtFile));

                gen.RepackSubtitleTemplate(voiceFile, @"C:\G\Wii\R79JAF patch assets\subtitleImgCutIn");

                return;

                var group = new string(voiceFile.Where(i => char.IsLetter(i)).Take(3).ToArray());

                //HACK for faster testing
                if (group is not "eve") return;
                if (voiceFile is not "eve011") return;

                //dont do it for music
                if (group is "bgm") return;
                //nor narrative
                if (group is "na") return;
                //or tutorial
                //if (group is "tut") return;
                //or exam robo shouts
                if (group is "exm") return;
                //or mobile suit variation descriptions :)
                if (group is "msv") return;

                //TODO filter out intermission and chat

                var avatar = group; //for pilots voice group matches avatar
                //TODO gather ev*->face mapping from GEV msg window calls. EVC will still need manual input
                if (group is "eva") avatar = "GSH"; //ace
                if (group is "eve") avatar = "HU1"; //eff
                if (group is "evs") avatar = "GSH"; //shared
                if (group is "evz") avatar = "HU2"; //zeon

                if (group is "tut") avatar = "GSH"; //tutorial

                //random minion battle chatter
                if (group is "sla") avatar = "HU1";
                if (group is "slb") avatar = "HU2";
                if (group is "slc") avatar = "HU1";
                if (group is "sld") avatar = "HU2";
                if (group is "sle") avatar = "HU1";
                if (group is "slf") avatar = "HU2";

                Console.WriteLine(voiceFile);

                avatar = @$"C:\G\Wii\R79JAF patch assets\SubtitleAssets\Avatars\{avatar}.png";

                var txt = File.ReadAllText(txtFile);

                var groupDir = targetDir + "/" + group;
                Directory.CreateDirectory(groupDir);

                var target = groupDir + "/" + voiceFile + ".png";

                MakeSubPng(target, txt, avatar, voiceFile, out var newBressFile);

                //TODO repack .brres into template .arc

                var templateArc = mU8.Deserialize(null, null, File.ReadAllBytes(@"C:\G\Wii\R79JAF patch assets\SubtitleAssets\SubtitlesImgCutinTemplate.arc").AsMemory(), ctx, out _);

                (templateArc["/arc/IC_CHR.brres"] as U8FileNode).File =
                    new RawBinaryFile()
                    {
                        Data = File.ReadAllBytes(newBressFile)
                    };

                var b = new ByteBuffer();
                mU8.Serialize(templateArc, b, ctx, out _);

                File.WriteAllBytes(newBressFile.Replace(".brres", ".arc"), b.GetData());
            });
        }

        private static IMarshalingContext PrepU8Marshaling(out ITypeMarshaler<U8File> mU8)
        {
            var store = new DefaultMarshalerStore();
            var rootCtx = new RootMarshalingContext(store);

            U8Marshaling.Register(store);

            mU8 = store.FindMarshaler<U8File>();

            return rootCtx;
        }

        static void MakeSubPng(string targetFilename, string text, string avatarPath, string voiceFileName, out string newBressFile)
        {
            var bmp = new Bitmap(512, 128);
            var g = Graphics.FromImage(bmp);

            g.FillRectangle(
                Brushes.Transparent, 0, 0, 512, 128
                );

            g.FillRectangle(Brushes.Black, 0, 0, 512, 128);

            var avatar = Image.FromFile(avatarPath);
            g.DrawImage(avatar, 0, 0, 128, 128);

            Font font1 = new Font("Arial", 22, FontStyle.Bold, GraphicsUnit.Point);

            RectangleF rectF1 = new RectangleF(128, 0, 512 - 128, 128);

            StringFormat format = new StringFormat();
            format.LineAlignment = StringAlignment.Center;
            format.Alignment = StringAlignment.Center;

            var dim = g.MeasureString(text, font1, 512 - 128, format);
            while (dim.Width > rectF1.Width || dim.Height > rectF1.Height)
            {
                font1 = new Font("Arial", font1.Size - 0.1F, FontStyle.Bold, GraphicsUnit.Point);
                dim = g.MeasureString(text, font1, 512 - 128, format);
            }

            g.DrawString(text, font1, Brushes.White, rectF1, format);

            bmp.Save(targetFilename, ImageFormat.Png);

            var brresDir = Path.GetDirectoryName(targetFilename) + "/" + Path.GetFileNameWithoutExtension(targetFilename);
            UnpacktemplateBrres(brresDir);

            ConvertToWiiTexture(targetFilename, brresDir + "/Textures(NW4R)/ImageCutIn_00");
            var texture = File.ReadAllBytes(brresDir + "/Textures(NW4R)/ImageCutIn_00");
            texture[11] = 0x01; //HACK Wiimm tools output tex0 v3, R79JAF is using v1 but its also not using any v3 features so just changing version number seems to work
            File.WriteAllBytes(brresDir + "/Textures(NW4R)/ImageCutIn_00", texture);

            var duration = GetBRSTMduration(BRSTMDir + "/" + voiceFileName + ".brstm");
            var framecount = (ushort)DurationSecondsToFrameCount(duration);

            var clrPath = brresDir + "/AnmClr(NW4R)/IMAGE_CUT_IN_00";
            //load constant-color CLR0
            var clr0template = File.ReadAllBytes(clrPath);

            //update frame count to match calculated
            BinaryPrimitives.WriteUInt16BigEndian(clr0template.AsSpan(0x1C), framecount);


            //and replace file extracted by Wiimm
            File.WriteAllBytes(clrPath, clr0template);

            newBressFile = Path.GetDirectoryName(targetFilename)
                + "/IC_" + Path.GetFileNameWithoutExtension(targetFilename)
                + ".brres";
            PackSubtitleBrres(brresDir, newBressFile);
        }
        const string BRSTMDir = @"C:\G\Wii\R79JAF_clean\DATA\files\sound\stream";

        static void ConvertToWiiTexture(string png, string targetFilename)
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
        const string tempalteBrresPath = @"C:\G\Wii\R79JAF patch assets\SubtitleAssets\SubtitlesImgCutinTemplate.brres";
        static void UnpacktemplateBrres(string targetDir)
        {
            ProcessStartInfo psi = new ProcessStartInfo(@"C:\Program Files\Wiimm\SZS\wszst.exe",
                $"EXTRACT \"{tempalteBrresPath}\" " +
                "--overwrite " +
                $"--DEST \"{targetDir}\"");
            var p = Process.Start(psi);
            p.WaitForExit();
        }
        static void PackSubtitleBrres(string sourceDir, string targetFile)
        {
            ProcessStartInfo psi = new ProcessStartInfo(@"C:\Program Files\Wiimm\SZS\wszst.exe",
                $"CREATE \"{sourceDir}\" " +
                "--overwrite " +
                $"--DEST \"{targetFile}\"");
            var p = Process.Start(psi);
            p.WaitForExit();
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

        static int DurationSecondsToFrameCount(double seconds, int frameRate = 60)
        {
            var SperF = 1.0 / frameRate;
            var frameCount = seconds / SperF;

            return (int)(frameCount);
        }
    }
}
