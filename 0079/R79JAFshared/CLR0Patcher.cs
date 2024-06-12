using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace R79JAFshared
{
    public static class CLR0Patcher
    {
        public static void PatchClr0(string path, int displayFrom, int displayTo, int frameCount = 6000, int startMask = 0x04_03_02_01)
        {
            var data = File.ReadAllBytes(path).AsSpan();

            for (int i = 0; i < data.Length; i += 4)
            {
                if (MemoryMarshal.Read<int>(data.Slice(i, 4)) == startMask)
                {
                    for (int j = 0; j < frameCount; j++)
                    {
                        data[i + j * 4 + 0] = 0;
                        data[i + j * 4 + 1] = 0;
                        data[i + j * 4 + 2] = 0;

                        if (j < displayFrom || j > displayTo)
                        {
                            data[i + j * 4 + 3] = 0;
                        }
                        else
                        {
                            data[i + j * 4 + 3] = 255; //alpha
                        }
                    }

                    break;
                }
            }

            File.WriteAllBytes(path, data.ToArray());
        }
    }
}
