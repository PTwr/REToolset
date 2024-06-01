using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R79JAFshared
{
    public static class ExternalToolsHelper
    {
        public static double GetBRSTMduration(string path)
        {
            if (File.Exists(path) is false)
                return 0;

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
    }
}
