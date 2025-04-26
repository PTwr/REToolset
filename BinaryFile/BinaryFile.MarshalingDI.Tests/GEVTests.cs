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

            builder
                .WithMagicString(GEV.OFSMagicNumber, gev => (gev.OFSDataOffset - 4, OffsetRelation.Segment));
            builder
                .WithCollection<ushort>(gev => gev.OFS, gev => gev.OFSDataOffset)
                .ReadInto((gev, data, l) =>
                {
                    gev.OFS = data.Select(x => x.Value).ToList();
                })
                .WithReadItemCountOf(gev => gev.OFSDataCount)
                .WithWriteOrderOf(200);

            builder
                .WithMagicString(GEV.STRMagicNumber, gev => (gev.STRDataOffset - 4, OffsetRelation.Segment));
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
                .WithWriteOrderOf(200);

            //TODO EVE
            builder
                .WithField(gev => gev.EVESegment, gev => (gev.EVEDataOffset - 4, OffsetRelation.Segment))
                //after OFS/STR gets deciphered
                .WithReadOrderOf(10);

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
                //TODO automaticaly find ctors by parent type?
                .WithActivator<GEV>(gev => new EVESegment(gev));

            builder
                .WithMagicString(GEV.EVEMagicNumber, eve => (0, OffsetRelation.Segment));

            builder
                .RegisterInDI(containerBuilder);

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

            Assert.Equal(cleanBytes, resultBytes);
        }
    }
}
