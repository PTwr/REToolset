using BinaryDataHelper;
using BinaryFile.Marshaling.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo.R79JAF.GEV
{
    public class GEV : IBinaryFile
    {
        public List<EVEOpCode> EVEOpCodes { get; set; }
        public EVESegment EVESegment { get; set; }

        public const string GEVMagicNumber = "$EVFEV02";
        public const string EVEMagicNumber = "$EVE";
        public const string OFSMagicNumber = "$OFS";
        public const string STRMagicNumber = "$STR";

        public string GEVMagic { get; set; } = GEVMagicNumber;
        public string EVEMagic { get; set; } = EVEMagicNumber;
        public string OFSMagic { get; set; } = OFSMagicNumber;
        public string STRMagic { get; set; } = STRMagicNumber;

        public int EVELineCount { get; set; }
        public int EVEDataOffset { get; set; }
        public int OFSDataCount { get; set; }
        //points to OFS content, skipping $OFS header
        public int OFSDataOffset { get; set; }
        public int STRDataOffset { get; set; }

        public byte[] EVEBytes { get; set; }

        public Dictionary<int, ushort> OFS { get; set; }
        public List<string> STR { get; set; }
    }
}
