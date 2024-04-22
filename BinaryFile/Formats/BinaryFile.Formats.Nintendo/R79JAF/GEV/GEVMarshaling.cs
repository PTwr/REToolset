using BinaryDataHelper;
using BinaryFile.Formats.Nintendo.R79JAF.GEV.EVELines;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Formats.Nintendo.R79JAF.GEV
{
    public static class GEVMarshaling
    {
        public static void Register(IMarshalerStore marshalerStore)
        {
            RegisterGev(marshalerStore);

            RegisterEveOpCode(marshalerStore);

            RegisterEveSegment(marshalerStore);

            RegisterEveBlock(marshalerStore);

            RegisterEveLine(marshalerStore);

            EVEJumpTable.Register(marshalerStore);
        }

        private static void RegisterGev(IMarshalerStore marshalerStore)
        {
            var gevMap = new RootTypeMarshaler<GEV>();
            marshalerStore.Register(gevMap);

            gevMap.BeforeSerialization((gev, data, ctx) =>
            {
                //TODO rethink, this is disgusting.
                //This could be done through fieldByteLength in Field AfterSerialize event
                //but it would require OFSDataOffset and STRDataOffset to be moved there as well
                //it could be all moved to EVESegment AfterSerialize
                //Either ugly code or logic placed in werid place :/
                gev.EVELineCount = gev.EVESegment.Blocks.Sum(i => i.EVELines.Count);
                var eveOpCodeCount =
                    gev.EVESegment.Blocks.Sum(i =>
                        i.EVELines.Sum(l =>
                            l.LineOpCodeCount //terminator is already inlcuded
                        )
                        + 1 //Block terminator
                    )
                    + 1; //EVE terminator
                gev.OFSDataOffset = gev.EVEDataOffset + GEV.OFSMagicNumber.Length + eveOpCodeCount * 4;
                gev.STRDataOffset = gev.STR == null ? gev.STRDataOffset : gev.OFSDataOffset + gev.STR.Count() * 2 + GEV.STRMagicNumber.Length;

                //For odd number of OFS entries a corrective alignment is required, as GEV file chunks are 32git aligned
                gev.STRDataOffset = gev.STRDataOffset.Align(4);
            });

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
                .WithField(i => i.EVEDataOffset)
                .AtOffset(8 + 4 * 1)
                .WithExpectedValueOf(0x20)
                .From(gev => 0x20);

            gevMap
                .WithField(i => i.OFSDataCount)
                .AtOffset(8 + 4 * 2)
                .WithSerializationOrderOf(150) //between STR and OFS serialization
                .From(gev => gev.OFS?.Count ?? gev.OFSDataCount);
            gevMap
                .WithField(i => i.OFSDataOffset)
                .AtOffset(8 + 4 * 3);
            gevMap
                .WithField(i => i.STRDataOffset)
                .AtOffset(8 + 4 * 4);

            gevMap
                .WithField(i => i.EVEMagic)
                .AtOffset(0x1C)
                .WithByteLengthOf(4)
                .WithExpectedValueOf(GEV.EVEMagicNumber)
                //TODO Allow Validation without Into?
                .From(root => GEV.EVEMagicNumber);

            gevMap
                .WithCollectionOf(i => i.EVEOpCodes, serialize: false)
                .AtOffset(0x1C + GEV.EVEMagicNumber.Length)
                .WithDeserializationOrderOf(10) //after header is read
                .WithByteLengthOf(gev =>
                {
                    return gev.EVEOpCodes?.Count * 4 ?? gev.OFSDataOffset - GEV.OFSMagicNumber.Length - 0x1C - GEV.EVEMagicNumber.Length;
                });

            gevMap
                .WithField(i => i.EVESegment)
                .AtOffset(0x1C)
                .WithDeserializationOrderOf(10);

            //TODO ValidateMagic helper method
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
                .AfterSerializingItem((gev, item, n, itemByteLength, itemOffset) =>
                {
                    //OFS holds index to 16bit (4byte) offsets in STR
                    gev.OFS[n] = (ushort)(itemOffset >> 2);
                });
        }

        private static void RegisterEveOpCode(IMarshalerStore marshalerStore)
        {
            var opCodeMap = new RootTypeMarshaler<EVEOpCode>();
            marshalerStore.Register(opCodeMap);

            opCodeMap.WithByteLengthOf(4);

            opCodeMap
                .WithField(i => i.Instruction)
                .AtOffset(0);
            opCodeMap
                .WithField(i => i.Parameter)
                .AtOffset(2);
        }

        private static void RegisterEveSegment(IMarshalerStore marshalerStore)
        {
            var eveSegmentMap = new RootTypeMarshaler<EVESegment>();
            marshalerStore.Register(eveSegmentMap);

            //TODO some helper for magic number validation
            eveSegmentMap
                .WithField(i => i.EVEMagic)
                .AtOffset(0)
                .WithExpectedValueOf(GEV.EVEMagicNumber)
                .WithByteLengthOf(GEV.EVEMagicNumber.Length)
                .From(eve => GEV.EVEMagicNumber);

            eveSegmentMap
                .WithCollectionOf(i => i.Blocks)
                .AtOffset(GEV.EVEMagicNumber.Length)
                //TODO pattern helper
                .BreakWhen((obj, items, data, ctx) =>
                {
                    var slice = ctx.ItemSlice(data).Span;
                    //stop reading blocks if next opcode is EVE terminator
                    return
                        slice[0] == 0x00 &&
                        slice[1] == 0x06 &&
                        slice[2] == 0xFF &&
                        slice[3] == 0xFF;
                });

            eveSegmentMap
                .WithField(i => i.Terminator)
                .WithValidator((block, terminator) => terminator.Instruction == 0x00006 && terminator.Parameter == 0xFFFF)
                .AtOffset(eve =>
                {
                    //for deserializatino it could be read from absolute (OFSDataOffset -4 -4)
                    var terminatorOffset = eve.Blocks.Sum(b => b.ByteLength) + GEV.EVEMagicNumber.Length;
                    return terminatorOffset;
                });
        }

        private static void RegisterEveBlock(IMarshalerStore marshalerStore)
        {
            var eveBlockMap = new RootTypeMarshaler<EVEBlock>();
            marshalerStore.Register(eveBlockMap);

            eveBlockMap
                .WithByteLengthOf(block => block.EVELines.Sum(i => i.LineOpCodeCount) * 4 + 4);

            eveBlockMap
                .WithCollectionOf(i => i.EVELines)
                .AtOffset(0)
                //TODO break when pattern helper method
                .BreakWhen((obj, items, data, ctx) =>
                {
                    var slice = ctx.ItemSlice(data).Span;
                    //stop reading lines if next opcode is block terminator
                    return
                        slice[0] == 0x00 &&
                        slice[1] == 0x05 &&
                        slice[2] == 0xFF &&
                        slice[3] == 0xFF;
                })
                .Into((block, x) => block.EVELines = x.ToList())
                .From(block => block.EVELines);

            eveBlockMap
                .WithField(i => i.Terminator)
                .WithValidator((block, terminator) => terminator.Instruction == 0x00005 && terminator.Parameter == 0xFFFF)
                .AtOffset(block => block.EVELines.Sum(i => i.LineOpCodeCount) * 4);
        }

        private static void RegisterEveLine(IMarshalerStore marshalerStore)
        {
            var eveLineMap = new RootTypeMarshaler<EVELine>();
            marshalerStore.Register(eveLineMap);

            eveLineMap
                .WithByteLengthOf(line => line.LineOpCodeCount * 4)
                .BeforeSerialization((line, l, ctx) =>
                {
                    line.Recompile();
                    line.JumpOffset = (ctx.ItemAbsoluteOffset - 0x20) / 4;
                })
                .AfterDeserialization((line, l, ctx) =>
                {
                    line.Decompile();
                    line.JumpOffset = (ctx.ItemAbsoluteOffset - 0x20) / 4;
                });

            eveLineMap
                .WithField(i => i.LineStartOpCode)
                .AtOffset(0)
                .WithValidator((line, opcode) => opcode.Instruction == 0x00001);
            eveLineMap
                .WithField(i => i.LineLengthOpCode)
                .AtOffset(4);

            eveLineMap
                .WithCollectionOf(i => i.Body)
                .WithDeserializationOrderOf(10) //has to be after LineLength
                .AtOffset(8)
                .WithCountOf(line => line.BodyOpCodeCount);

            eveLineMap
                .WithField(i => i.Terminator)
                //TODO niceer OpCode validation. opcode.IsLineTerminator? or terminator == 0x00040000 through implicit cast?
                //TODO store opcode values in consts
                .WithValidator((block, terminator) => terminator.Instruction == 0x00004 && terminator.Parameter == 0x0000)
                //last opcode of full line
                .AtOffset(line => line.LineOpCodeCount * 4 - 4);
        }
    }
}
