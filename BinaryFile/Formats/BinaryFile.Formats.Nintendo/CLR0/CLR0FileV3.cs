using BinaryFile.Marshaling.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo.CLR0
{
    public class CLR0File : IBinaryFile
    {
        public string Magic { get; set; } = "CLR0";

        public int SubFileLength { get; set; }
        public int SubFileVersion { get; set; } = 3;

        public int OuterBrresOffset { get; set; }

        public int DataOffset { get; set; }
        public int FileNameOffset { get; set; }
        public string FileName { get; set; }

        public CLR0FileV3 V3 { get; set; }
    }

    public class CLR0FileV3
    {
        public CLR0FileV3(CLR0File parent)
        {
            Parent = parent;
        }
        public int Unused { get; set; } = 0;
        public short FrameCount { get; set; }
        public short MaterialsCount { get; set; }
        public bool LoopingEnabled { get; set; } //as 32bit

        public List<CLR0MaterialData> MaterialData { get; set; }

        public CLR0File Parent { get; }
    }

    public class CLR0MaterialData
    {
        public CLR0MaterialData(CLR0FileV3 parent)
        {
            Parent = parent;
        }

        public int TargetNameOffset { get; set; }
        public int AnimationFlag { get; set; }

        public CLR0AnimationData AnimationData { get; set; }
        public CLR0FileV3 Parent { get; }
    }
    public class CLR0AnimationData
    {
        public CLR0AnimationData(CLR0MaterialData parent)
        {
            Parent = parent;
        }
        public RGBA Mask { get; set; }

        public int DataOffset { get; set; }

        public int DataCount { get; set; }
        public List<RGBA> Data { get; set; }
        public CLR0MaterialData Parent { get; }
    }

    public class BrresIndexGroup
    {
        public uint ByteLength { get; set; }
        public uint ItemCount { get; set; }
    }

    public class BrresIndexItem
    {
        public ushort Id { get; set; }
    }

    public class RGBA
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        public override string ToString()
        {
            return $"{R:X2}{G:X2}{B:X2}{A:X2}";
        }
    }

}
