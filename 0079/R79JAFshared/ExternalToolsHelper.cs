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
        public static bool Verbose = false;
        public static Dictionary<string, double> DurationCache = new();
        public static double GetBRSTMduration(string path)
        {
            if (File.Exists(path) is false)
            {
                if (Verbose) Console.WriteLine($"File not found '{path}'");
                return 0;
            }

            if (DurationCache.TryGetValue(path, out var cached))
            {
                if (Verbose) Console.WriteLine($"FFprobe of {path} returns duration of {cached} (cached)");
                return cached;
            }

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

            if (Verbose) Console.WriteLine($"FFprobe of {path} returns duration of {duration}");

            DurationCache[path] = duration;

            return duration;
        }
    }
}
