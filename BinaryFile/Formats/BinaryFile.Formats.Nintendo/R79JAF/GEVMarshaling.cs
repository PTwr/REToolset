using BinaryDataHelper;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo.R79JAF
{
    public static class GEVMarshaling
    {
        public static void Register(IMarshalerStore marshalerStore)
        {
            var gevMap = new RootTypeMarshaler<GEV>();
            marshalerStore.Register(gevMap);

            gevMap
                .WithField(i => i.GEVMagic)
                .AtOffset(0)
                .WithByteLengthOf(8)
                .WithExpectedValueOf(GEV.GEVMagicNumber)
                .From(root => GEV.GEVMagicNumber);

            gevMap
                .WithField(i => i.EVELineCount)
                .AtOffset(8 + 4 * 0)
                .Into((gev, x) => gev.EVELineCount = x)
                .From(gev => gev.EVELineCount);
            gevMap
                //TODO This is probably offset to EVE body, OFS and STR does that "skip the magic" thing as well!
                .WithField(i => i.EVEDataOffset)
                .AtOffset(8 + 4 * 1)
                .WithExpectedValueOf(0x20)
                .From(gev => 0x20);

            gevMap
                .WithField(i => i.OFSDataCount)
                .AtOffset(8 + 4 * 2)
                .WithSerializationOrderOf(150) //between STR and OFS serialization
                .From(gev => gev.OFS?.Count ?? gev.OFSDataCount);
            gevMap //TODO calculate from EVE length
                .WithField(i => i.OFSDataOffset)
                .AtOffset(8 + 4 * 3);
            gevMap
                .WithField(i => i.STRDataOffset)
                .AtOffset(8 + 4 * 4)
                //TODO after OFSDataOffset gets recalculated from EVE
                .From(gev =>
                {
                    //TODO rethink, that should go to some kind of RecalculateBeforeSerialization
                    gev.STRDataOffset = gev.STR == null ? gev.STRDataOffset : (gev.OFSDataOffset + gev.STR.Count() * 2 + GEV.STRMagicNumber.Length);
                    return gev.STRDataOffset;
                });

            gevMap
                .WithField(i => i.EVEMagic)
                .AtOffset(0x1C)
                .WithByteLengthOf(4)
                .WithExpectedValueOf(GEV.EVEMagicNumber)
                //TODO Allow Validation without Into?
                .From(root => GEV.EVEMagicNumber);

            gevMap
                .WithCollectionOf(i => i.EVEOpCodes)
                .AtOffset(0x1C + GEV.EVEMagicNumber.Length)
                .WithDeserializationOrderOf(10) //after header is read
                .WithItemByteLengthOf(4)
                .WithByteLengthOf(gev =>
                {
                    return gev.EVEOpCodes?.Count * 4 ?? (gev.OFSDataOffset - GEV.OFSMagicNumber.Length - 0x1C - GEV.EVEMagicNumber.Length);
                });

            //TODO ValidateMagic
            gevMap
                .WithField(i => i.OFSMagic)
                .AtOffset(eve => eve.OFSDataOffset - 4) //this points to OFS data, skipping magic
                .WithNullTerminator(false)
                .WithByteLengthOf(4)
                .WithExpectedValueOf(GEV.OFSMagicNumber)
                .WithSerializationOrderOf(200) //after STR, awaiting for offset update
                .From(root => GEV.OFSMagicNumber);

            //TODO conditional deserializatoin, this section is optional (but header stays)
            gevMap
                .WithCollectionOf<ushort>("OFS", null, false, false)
                .AtOffset(eve => eve.OFSDataOffset)
                .WithCountOf(gev => gev.OFSDataCount)
                .Into((gev, x) => gev.OFS = x.Select((x, n) => new { x, n }).ToDictionary(x => x.n, x => x.x))
                .WithSerializationOrderOf(200) //after STR, awaiting for offset update
                .From(gev => gev.OFS.Values.AsEnumerable());

            //TODO conditional deserializatoin, this magic is optional
            gevMap
                .WithField(i => i.STRMagic)
                .AtOffset(eve => eve.STRDataOffset - 4) //this points to STR data, skipping magic
                .WithByteLengthOf(4)
                .WithExpectedValueOf(GEV.STRMagicNumber)
                //TODO Allow Validation without Into?
                .Into((gev, x) => { })
                .From(root => GEV.STRMagicNumber);

            //TODO conditional deserialization, this section is optional
            gevMap
                .WithCollectionOf(i => i.STR)
                .WithSerializationOrderOf(100)
                .AtOffset(eve => eve.STRDataOffset)
                .WithItemNullPadToAlignment(4)
                .WithNullTerminator(true)
                .WithEncoding(BinaryStringHelper.Shift_JIS)
                //no length, this goes until the end
                .Into((gev, x) => gev.STR = x.ToList())
                .From(gev => gev.STR)
                .AfterSerializingItem((GEV gev, string item, int n, int itemByteLength, int itemOffset) =>
                {
                    //OFS holds index to 16bit (4byte) offsets in STR
                    gev.OFS[n] = (ushort)(itemOffset >> 2);
                });

            /////////////////////////////////////////////////////////////////////////////

            var opCodeMap = new RootTypeMarshaler<EVEOpCode>();
            marshalerStore.Register(opCodeMap);

            opCodeMap
                .WithField(i => i.Instruction)
                .AtOffset(0);
            opCodeMap
                .WithField(i => i.Parameter)
                .AtOffset(2);

        }
    }
}
