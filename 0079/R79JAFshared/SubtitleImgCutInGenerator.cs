using System.Drawing;
using System.Drawing.Imaging;

namespace R79JAFshared
{
    public class SubtitleImgCutInGenerator
    {
        public const int RenderWidth = 512;
        public const int RenderHeight = 512;
        private readonly string subtitleAssetsDirectory;

        public SubtitleImgCutInGenerator(string subtitleAssetsDirectory)
        {
            this.subtitleAssetsDirectory = subtitleAssetsDirectory;
        }

        public void PrepareSubtitleImage(string pilotCode, string text, string targetPath, int subtitleHeight = 128)
        {
            var bmp = new Bitmap(512, 512);
            var g = Graphics.FromImage(bmp);

            g.FillRectangle(
                Brushes.Transparent, 0, 0, RenderWidth, RenderHeight
                );

            g.FillRectangle(Brushes.Black, 0, 0, RenderWidth, subtitleHeight);

            var avatar = Image.FromFile($"{subtitleAssetsDirectory}/Avatars/{pilotCode}.png");
            //TODO counteract 4:3 distortion?
            g.DrawImage(avatar, 0, 0, subtitleHeight, subtitleHeight*0.75f);

            Font font1 = new Font("Arial", 22, FontStyle.Bold, GraphicsUnit.Point);

            RectangleF rectF1 = new RectangleF(subtitleHeight, 0, RenderWidth - subtitleHeight, subtitleHeight);

            StringFormat format = new StringFormat();
            format.LineAlignment = StringAlignment.Center;
            format.Alignment = StringAlignment.Center;

            var dim = g.MeasureString(text, font1, RenderWidth - 128, format);
            while (dim.Width > rectF1.Width || dim.Height > rectF1.Height)
            {
                font1 = new Font("Arial", font1.Size - 0.1F, FontStyle.Bold, GraphicsUnit.Point);
                dim = g.MeasureString(text, font1, RenderWidth - 128, format);
            }

            g.DrawString(text, font1, Brushes.White, rectF1, format);

            bmp.Save(targetFilename, ImageFormat.Png);
        }
    }
}
