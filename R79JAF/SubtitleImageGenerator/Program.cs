using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

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

                MakeSubPng(target, txt, avatar);
            });
        }


        static void MakeSubPng(string targetFilename, string text, string avatarPath)
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
            ConvertToWiiTexture(targetFilename, targetFilename.Replace(".png", ".texture"));
        }

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
    }
}
