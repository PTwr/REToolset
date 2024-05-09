using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;

namespace SubtitleImageGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var targetDir = @"C:\G\Wii\R79JAF patch assets\CutinSubtitleTextures";
            var translationRoot = @"C:\G\Wii\R79JAF patch assets\sound_translate\stream";
            var txtFiles = Directory.EnumerateFiles(translationRoot, "*.brstm.txt", SearchOption.AllDirectories);

            Parallel.ForEach(txtFiles, (txtFile) =>
            {
                //remove .brstm.txt
                var voiceFile = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(txtFile));
                var group = new string(voiceFile.Where(i => char.IsLetter(i)).Take(3).ToArray());

                //HACK for faster testing
                if (group is not "eve") return;
                if (voiceFile is not "eve011") return;

                //dont do it for music
                if (group is "bgm") return;
                //nor narrative
                if (group is "na") return;
                //or tutorial
                if (group is "tut") return;
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

                //random minion battle chatter
                if (group is "sla") avatar = "HU1";
                if (group is "slb") avatar = "HU2";
                if (group is "slc") avatar = "HU1";
                if (group is "sld") avatar = "HU2";
                if (group is "sle") avatar = "HU1";
                if (group is "slf") avatar = "HU2";

                Console.WriteLine(voiceFile);

                avatar = @$"C:\G\Wii\R79JAF patch assets\Avatars\{avatar}_MSG_Window_01.png";

                var txt = File.ReadAllText(txtFile);

                var groupDir = targetDir + "/" + group;
                Directory.CreateDirectory(groupDir);

                var target = groupDir + "/" + voiceFile + ".png";

                MakeSubPng(target, txt, avatar, voiceFile);
            });
        }


        static void MakeSubPng(string targetFilename, string text, string avatarPath, string voiceFileName)
        {
            var bmp = new Bitmap(512, 512);
            var g = Graphics.FromImage(bmp);

            g.FillRectangle(
                Brushes.Transparent, 0, 0, 512, 512
                );

            g.FillRectangle(Brushes.Black, 0, 0, 512, 127);

            var avatar = Image.FromFile(avatarPath);
            g.DrawImage(avatar, 0, 0, 127, 127);

            Font font1 = new Font("Arial", 22, FontStyle.Bold, GraphicsUnit.Point);

            RectangleF rectF1 = new RectangleF(127, 0, 512 - 127, 127);

            StringFormat format = new StringFormat();
            format.LineAlignment = StringAlignment.Center;
            format.Alignment = StringAlignment.Center;

            var dim = g.MeasureString(text, font1, 512 - 127, format);
            while (dim.Width > rectF1.Width || dim.Height > rectF1.Height)
            {
                font1 = new Font("Arial", font1.Size - 0.1F, FontStyle.Bold, GraphicsUnit.Point);
                dim = g.MeasureString(text, font1, 512 - 127, format);
            }

            g.DrawString(text, font1, Brushes.White, rectF1, format);

            bmp.Save(targetFilename, ImageFormat.Png);

            var brresDir = Path.GetDirectoryName(targetFilename) + "/" + Path.GetFileNameWithoutExtension(targetFilename);
            UnpacktemplateBrres(brresDir);

            ConvertToWiiTexture(targetFilename, brresDir + "/Textures(NW4R)/ImageCutIn_00");

            var duration = GetBRSTMduration(BRSTMDir + "/" + voiceFileName + ".brstm");
            var framecount = DurationSecondsToFrameCount(duration);

            PackSubtitleBrres(brresDir);
        }
        const string BRSTMDir = @"C:\G\Wii\R79JAF_clean\DATA\files\sound\stream";

        static void ConvertToWiiTexture(string png, string targetFilename)
        {
            ProcessStartInfo psi = new ProcessStartInfo(@"C:\Program Files\Wiimm\SZS\wimgt.exe",
                $"ENCODE \"{png}\" " +
                "--overwrite " +
                "--transform CMPR " +
                $"--DEST \"{targetFilename}\"");
            var p = Process.Start(psi);
            p.WaitForExit();
        }
        const string tempalteBrresPath = @"C:\G\Wii\R79JAF patch assets\SubtitlesImgCutinTemplate.brres";
        static void UnpacktemplateBrres(string targetDir)
        {
            ProcessStartInfo psi = new ProcessStartInfo(@"C:\Program Files\Wiimm\SZS\wszst.exe",
                $"EXTRACT \"{tempalteBrresPath}\" " +
                "--overwrite " +
                $"--DEST \"{targetDir}\"");
            var p = Process.Start(psi);
            p.WaitForExit();
        }
        static void PackSubtitleBrres(string targetDir)
        {
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
