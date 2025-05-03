using Autofac;
using BinaryDataHelper;
using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.Formats.Nintendo.R79JAF.GEV;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.DI;
using BinaryFile.MarshalingDI.Files;
using BinaryFile.MarshalingDI.Marshaling.Complex;
using BinaryFile.MarshalingDI.Marshaling.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Tests
{
    public class GEVTests
    {
        string tr01gev_clean = @"C:\G\Wii\R79JAF_clean\DATA\files\event\missionevent\other\TR01.gev";
        string tr01gev_dirty = @"C:\G\Wii\R79JAF_dirty\DATA\files\event\missionevent\other\TR01.gev";
        public static IContainer Setup()
        {
            var cb = new ContainerBuilder()
                .WithRequiredServices(useOptimizedServices: false)
                .WithHelpers()
                .WithPrimitiveMarshalers();
            SetupGEV(cb);
            SetupEVE(cb);
            SetupEVEBlock(cb);
            SetupEVELine(cb);
            SetupEVELineBodies(cb);
            SetupEVEOpCodes(cb);
            return cb.Build();
        }

        public static void SetupGEV(ContainerBuilder containerBuilder = null)
        {
            containerBuilder ??= new ContainerBuilder()
                .WithRequiredServices(useOptimizedServices: false)
                .WithHelpers()
                .WithPrimitiveMarshalers();

            var builder = new ObjectBuilder<GEV>()
                .InBigEndian();

            builder
                //TODO unify it somehow? But BytePattern is for Marshaler selection, while Magic string Writes magic to output
                .ForBytePatternOf(GEV.GEVMagicNumber)
                .WithMagicString(GEV.GEVMagicNumber);

            /////////////////////////////header
            builder
                .WithField(x => x.EVELineCount, 8);
            //TODO const => Magic?
            builder
                .WithField(x => x.EVEDataOffset, 8 + 4 * 1)
                .WithExpectedValueOf(0x20);
            builder
                .WithField(x => x.OFSDataCount, 8 + 4 * 2)
                .WithWriteOrderOf(150);
            builder
                .WithField(x => x.OFSDataOffset, 8 + 4 * 3);
            builder
                .WithField(x => x.STRDataOffset, 8 + 4 * 4);

            /////////////////////////////body

            //TODO if OFSDataCount/OFSDataOffset/STRDataOffset == 0 $STR magic is still present at the end of file
            builder
                //.WithMagicString(GEV.OFSMagicNumber, gev => (gev.OFSDataOffset - 4, OffsetRelation.Segment))
                .WithMagicString(GEV.OFSMagicNumber)
                .AtOffset((scope) =>
                {
                    var gev = scope.GetFeatures().GetCurentObject<GEV>();
                    var io = scope.Resolve<IDataBuffer>();

                    //TODO flag to tell whether it is Read or Write offset calculation?
                    //for voice gevs its io.length-4 when reading, but io.length when writing!
                    if (gev.EVESegment is not null && gev.OFSDataOffset == 0)
                    {
                        return (io.Length, OffsetRelation.Segment);
                    }
                    if (gev.OFSDataOffset == 0)
                    {
                        return (io.Length - 4, OffsetRelation.Segment);
                    }
                    return (gev.OFSDataOffset - 4, OffsetRelation.Segment);
                })
                //TODO WriteAfterProperty(x=>x.aaa) helper? Autogen (overidable) Tag from getter Expression, build auto order by name?
                //write after EVE
                .WithWriteOrderOf(11);
            builder
                .WithCollection<ushort>(gev => gev.OFS, gev => gev.OFSDataOffset)
                .ReadInto((gev, data, l) =>
                {
                    gev.OFS = data.Select(x => x.Value).ToList();
                })
                .WithReadItemCountOf(gev => gev.OFSDataCount)
                .WithWriteOrderOf(200)
                .IsFor((c) =>
                {
                    var gev = c.GetFeatures().GetCurentObject<GEV>();

                    if (gev.OFSDataCount == 0)
                    {
                        return Marshaling.EMarshalingType.Writing;
                    }

                    return Marshaling.EMarshalingType.ReadWrite;
                });

            //TODO optional if OFSDataCount/OFSDataOffset/STRDataOffset == 0
            builder
                .WithMagicString(GEV.STRMagicNumber, gev => (gev.STRDataOffset - 4, OffsetRelation.Segment))
                .IsFor((c) =>
                {
                    var gev = c.GetFeatures().GetCurentObject<GEV>();

                    if (gev.OFSDataCount == 0)
                    {
                        return Marshaling.EMarshalingType.Writing;
                    }

                    return Marshaling.EMarshalingType.ReadWrite;
                });
            builder
                .WithCollection<string>(gev => gev.STR, gev => gev.STRDataOffset)
                .ReadInto((gev, data, l) =>
                {
                    gev.STR = data.Select(x => x.Value!).ToList();
                })
                .WithReadItemCountOf(gev => gev.OFSDataCount)
                .WithReadWriteMetadata(EStringLengthStyle.NullTerminator)
                .WithReadWriteMetadata(BinaryStringHelper.Shift_JIS)
                //TODO padding/alignment
                //.WithReadWriteMetadata<int>(4, nameof(EMetadataNames.Alignment)) - required for writing
                //TODO WithItemOffset<T>(TDeclaringType, int itemNumber) - calculating offsets through OFS would be enough for reading
                .WithReadMetadata<IPadding>((f, s) => new Padding(f, s, (ff, ss, bytesRead) =>
                {
                    var missingPad = (bytesRead % 4 > 0) ? (4 - bytesRead % 4) : 0;
                    return (missingPad, false);
                }), true)
                .WithWriteMetadata<IPadding>((f, s) => new Padding(f, s, (ff, ss, bytesRead) =>
                {
                    var missingPad = (bytesRead % 4 > 0) ? (4 - bytesRead % 4) : 0;
                    return (missingPad, true);
                }), true)
                .WithWriteOrderOf(200)
                .IsFor((c) =>
                {
                    var gev = c.GetFeatures().GetCurentObject<GEV>();

                    if (gev.OFSDataCount == 0)
                    {
                        return Marshaling.EMarshalingType.Writing;
                    }

                    return Marshaling.EMarshalingType.ReadWrite;
                });

            //TODO EVE
            builder
                .WithField(gev => gev.EVESegment, gev => (gev.EVEDataOffset - 4, OffsetRelation.Segment))
                //after OFS/STR gets deciphered
                .WithReadOrderOf(10)
                .WithWriteOrderOf(10);

            builder
                .RegisterInDI(containerBuilder);
        }

        public static void SetupEVE(ContainerBuilder containerBuilder = null)
        {
            containerBuilder ??= new ContainerBuilder()
                .WithRequiredServices(useOptimizedServices: false)
                .WithHelpers()
                .WithPrimitiveMarshalers();

            var builder = new ObjectBuilder<EVESegment>()
                .InBigEndian()
                //TODO automaticaly find ctors by parent type? previous marshaling had this so this is a regresion in feature set?
                .WithActivator<GEV>(gev => new EVESegment(gev));

            builder
                .WithMagicString(GEV.EVEMagicNumber, eve => (0, OffsetRelation.Segment));

            builder
                .WithCollection(eve => eve.Blocks, GEV.EVEMagicNumber.Length)
                //TODO make "ReadUntilPattern" helper, based on more generic "ReadUntil"?
                .WithReadMetadata<bool>((f, c) =>
                {
                    var buffer = c.Resolve<IDataBuffer>();
                    var offset = c.Resolve<IOffsetStack>().CurrentAbsoluteOffset;

                    //TODO Some gev's (eg. AS01) have no 0005FFFF before 0006FFFF
                    //which fucks up assumption that its terminator
                    bool isLineHeader = buffer.AsSpan(offset).StartsWith([00, 01]);
                    bool isExitOpCode = buffer.AsSpan(offset).StartsWith(EVEOpCode.SegmentTerminator);

                    return isLineHeader && !isExitOpCode;
                }, true, nameof(EMetadataNames.CollectionReadWhile), 0);

            //TODO figure bytelength math: magic + lines + block terminators + segment terminator
            builder
                .WithByteLengthOf(eve => 4 + eve.Blocks.Sum(block => block.EVELines.Sum(i => i.LineOpCodeCount) * 4 + 4) + 4);

            //Some GEV's (eg. AS01) have weird last line, no no 00 05 FF FF in ending, which fucks up calculations for position of 00 06 FF FF

            //builder
            //    .WithMagicOf(EVEOpCode.SegmentTerminator, eve => (
            //    eve.Blocks.Sum(block => block.EVELines.Sum(i => i.LineOpCodeCount) * 4 + 4) + 4
            //    , OffsetRelation.Segment));
            builder
                .WithMagicOf(EVEOpCode.SegmentTerminator, eve => (
                eve.Parent.OFSDataOffset - 4 - 4 //minus lengths of $OFS and 0006FFFF
                , OffsetRelation.Parent))
                .AtOffset((scope) =>
                {
                    var gev = scope.GetFeatures().GetParent<GEV>();
                    var eve = scope.GetFeatures().GetCurentObject<EVESegment>();
                    var io = scope.Resolve<IDataBuffer>();

                    return (eve.ByteLength - 4, OffsetRelation.Segment);
                });

            builder
                .RegisterInDI(containerBuilder);
        }

        public static void SetupEVEBlock(ContainerBuilder containerBuilder = null)
        {
            containerBuilder ??= new ContainerBuilder()
                .WithRequiredServices(useOptimizedServices: false)
                .WithHelpers()
                .WithPrimitiveMarshalers();

            var builder = new ObjectBuilder<EVEBlock>()
                .InBigEndian()
                //TODO automaticaly find ctors by parent type?
                .WithActivator<EVESegment>(eve => new EVEBlock(eve))
                .WithByteLengthOf(block => block.EVELines.Sum(i => i.LineOpCodeCount) * 4 + 4);

            builder
                //TODO discourage not providing Offset = 0, to avoid mistakes?
                .WithCollection(block => block.EVELines, 0)
                //TODO make "ReadUntilPattern" helper, based on more generic "ReadUntil"?
                //TODO what if there can be multiple stop patterns? helper with params arg?
                .WithReadMetadata<bool>((f, c) =>
                {
                    //TODO helper (ext?) methods for peeking at data slice
                    var buffer = c.Resolve<IDataBuffer>();
                    var offset = c.Resolve<IOffsetStack>().CurrentAbsoluteOffset;

                    return
                        buffer.AsSpan(offset).StartsWith(EVEOpCode.BlockTerminator) == false
                        &&
                        buffer.AsSpan(offset).StartsWith(EVEOpCode.SegmentTerminator) == false
                        ;
                }, true, nameof(EMetadataNames.CollectionReadWhile), 0);

            //TODO treat both 0005FFFF0006FFFF and 0006FFFF as Block terminator?
            builder
                //TODO MultiMagic??!? 
                .WithField(block => block.Terminator, block => (block.EVELines.Sum(i => i.LineOpCodeCount) * 4, OffsetRelation.Segment))
                .WithAfterReadValidator((scope) => scope.GetFeatures().GetCurentObject<EVEOpCode>() == EVEOpCode.BlockTerminator || scope.GetFeatures().GetCurentObject<EVEOpCode>() == EVEOpCode.SegmentTerminator);

            builder
                .RegisterInDI(containerBuilder);
        }

        public static void SetupEVELine(ContainerBuilder containerBuilder = null)
        {
            containerBuilder ??= new ContainerBuilder()
                .WithRequiredServices(useOptimizedServices: false)
                .WithHelpers()
                .WithPrimitiveMarshalers();

            var builder = new ObjectBuilder<EVELine>()
                .InBigEndian()
                //TODO automaticaly find ctors by parent type? previous marshaling had this so this is a regresion in feature set?
                .WithActivator<EVEBlock>(block => new EVELine(block))
                .ForBytePatternOf([0x00, 0x01, null, null]);

            builder
                .WithByteLengthOf(line => line.LineOpCodeCount * 4);

            //TODO separate classes for header stuff? Or Just fields? Why bother with subclasses for (mostly)known header?
            builder
                .WithField(line => line.LineStartOpCode, 0)
                //TODO easier helper to check current object
                //TODO ensure this is called!
                //TODO this could be Magic
                .WithAfterReadValidator((scope) => scope.GetFeatures().GetCurentObject<EVEOpCode>().HighWord == 1);
            builder
                .WithField(line => line.LineLengthOpCode, 4);

            //builder
            //    .WithCollection(line => line.Body, 8)
            //    //TODO WithHeaderField/WithBodyField helpers to provide automatic order for basic split?
            //    .WithReadOrderOf(10) //after Header
            //    .WithReadItemCountOf(line => line.BodyOpCodeCount);

            builder
                //TODO WithHeaderField/WithBodyField helpers to provide automatic order for basic split?
                .WithField(line => line.LineBody, 8)
                //TODO or AfterField(lambda) ?
                .WithReadOrderOf(10); //after header

            builder
                .WithMagicOf<uint>(EVEOpCode.LineTerminator, line => (line.LineOpCodeCount * 4 - 4, OffsetRelation.Segment));

            builder
                .RegisterInDI(containerBuilder);
        }

        public static void SetupEVELineBodies(ContainerBuilder containerBuilder = null)
        {
            containerBuilder ??= new ContainerBuilder()
                .WithRequiredServices(useOptimizedServices: false)
                .WithHelpers()
                .WithPrimitiveMarshalers();

            var defaultLine = new ObjectBuilder<EVELineRawBody>()
                .AlsoActivateFor<IEVELineBody>()
                .WithDefaultActivator<EVELine>(line => new EVELineRawBody(line))
                .InBigEndian();

            defaultLine
                .WithCollection(body => body.OpCodes, 0)
                .WithReadItemCountOf(body => body.Parent.BodyOpCodeCount);

            defaultLine
                .RegisterInDI(containerBuilder);

            var jumpTable = new ObjectBuilder<EVEJumpTableLine>()
                .ForBytePatternOf(EVEJumpTableLine.Mask)
                .AlsoActivateFor<IEVELineBody>()
                .WithDefaultActivator<EVELine>(line => new EVEJumpTableLine(line))
                .InBigEndian();

            jumpTable
                .WithMagicOf<uint>(0x00030000, 0);
            jumpTable
                .WithMagicOf<ushort>(0x0014, 4);

            //this might be some kind of method call or jump, appears to be opcode index of resource load line in typical mission gev, resource load line then 
            jumpTable
                .WithFieldOf<ushort>()
                .AtOffset(6)
                .WriteFrom(line => (ushort)(line.RawJumpTable.Count() * 2 + 6))
                .ReadInto((line, x)=> { });

            jumpTable
                .WithCollectionOf<EVEOpCode>()
                .AtOffset(8) //skip jumptable declaration
                .ReadInto((line, opcodes, bytesRead) =>
                {
                    line.RawJumpTable = new List<(ushort jumpId, ushort targetOpCode)>(opcodes.Count() / 2);
                    for (int i = 0; i < opcodes.Count; i += 2)
                    {
                        if (opcodes[i].Value!.HighWord != 0x0013) throw new InvalidOperationException();
                        if (opcodes[i+1].Value!.LowWord != 0xFFFF) throw new InvalidOperationException();
                        line.RawJumpTable.Add((opcodes[i + 1].Value!.HighWord, opcodes[i].Value!.LowWord));
                    }
                })
                .WriteFrom(line =>
                {
                    return line.RawJumpTable
                        .SelectMany(raw => new EVEOpCode[] {
                            new EVEOpCode(0x0013,raw.targetOpCode),
                            new EVEOpCode(raw.jumpId, 0xFFFF),
                        });
                })
                //assuming it fills whole line
                //TODO check if its valid in all gevs
                .WithReadItemCountOf(body => body.Parent.BodyOpCodeCount - 2);

            jumpTable
                .RegisterInDI(containerBuilder);
        }

        public static void SetupEVEOpCodes(ContainerBuilder containerBuilder = null)
        {
            containerBuilder ??= new ContainerBuilder()
                .WithRequiredServices(useOptimizedServices: false)
                .WithHelpers()
                .WithPrimitiveMarshalers();

            var builder = new ObjectBuilder<EVEOpCode>()
                .InBigEndian()
                //TODO automaticaly find ctors by parent type? previous marshaling had this so this is a regresion in feature set?
                //TODO rethin Parent detection, for Unary Fields maxAge = 1 is ok, but for Colelction 2 was needed somewhere in XBF >.<. So if ctor with parent=Block woudl be first, it would react before Line ctor 
                .WithActivator<EVELine>(line => new EVEOpCode(line))
                .WithActivator<EVEBlock>(block => new EVEOpCode(block));

            builder
                .WithField(x => x.HighWord, 0);
            builder
                .WithField(x => x.LowWord, 2);
            //required when reading OpCodes as list
            builder
                .WithByteLengthOf(4);

            //TODO builder.Clone() for more complex opcodes?

            builder
                .RegisterInDI(containerBuilder);
        }

        [Fact]
        public void ReadWriteLoopEverything()
        {
            var c = Setup();
            c.Resolve<IDataBufferIO>().EnableResize();

            foreach (var file in Directory.EnumerateFiles(@"C:\G\Wii\R79JAF_clean\DATA\files\event", "*.gev", SearchOption.AllDirectories)
                .Where(x => true || x.Contains("AS01")))
            {
                var cleanBytes = File.ReadAllBytes(file);

                c.Resolve<IDataBufferIO>().SetData(cleanBytes);

                var readHelper = c.Resolve<ReadHelper>();

                var gev = readHelper.Read<GEV>(out _);

                var writeHelper = c.Resolve<WriteHelper>();

                c.Resolve<IDataBufferIO>().SetData([]);

                writeHelper.Write(gev, out _);

                var resultBytes = c.Resolve<IDataBufferIO>().GetData();

                File.WriteAllBytes(@"c:\dev\a.bin", cleanBytes);
                File.WriteAllBytes(@"c:\dev\b.bin", resultBytes);

                Assert.Equal(cleanBytes, resultBytes);

                //TODO rethink, Decompilation into opcodes is shitting its breeches without eg. EVEJumpTable marshaled into exact class so everything migh as well be done during marshaling?

                gev.EVESegment.Decompile();

                var ss = gev.EVESegment.Blocks
                    .SelectMany(x => x.EVELines)
                    .Select(x => x.ToString());

                // File.WriteAllLines(file + ".txt", ss);
            }
        }

        [Fact]
        public void ReadWriteLoop()
        {
            var c = Setup();

            var cleanBytes = File.ReadAllBytes(tr01gev_clean);

            c.Resolve<IDataBufferIO>().SetData(cleanBytes);

            var readHelper = c.Resolve<ReadHelper>();

            var gev = readHelper.Read<GEV>(out _);

            var writeHelper = c.Resolve<WriteHelper>();

            c.Resolve<IDataBufferIO>().SetData([]);
            c.Resolve<IDataBufferIO>().EnableResize();

            writeHelper.Write(gev, out _);

            var resultBytes = c.Resolve<IDataBufferIO>().GetData();

            File.WriteAllBytes(@"c:\dev\a.bin", cleanBytes);
            File.WriteAllBytes(@"c:\dev\b.bin", resultBytes);

            Assert.Equal(cleanBytes, resultBytes);
        }
    }
}
