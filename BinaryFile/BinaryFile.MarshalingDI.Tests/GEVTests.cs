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

            //TODO helper for WithMagicString and WithMagicNumber? Allow for validation and writing witohut backing field?
            builder
                .WithField(x => x.GEVMagic, 0)
                .WithExpectedValueOf(GEV.GEVMagicNumber)
                .WithReadWriteMetadata(Encoding.ASCII)
                .WithReadWriteMetadata<int>(GEV.GEVMagicNumber.Length, nameof(EMetadataNames.StringLength));

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
                .WithField(x => x.OFSMagic, gev => (gev.OFSDataOffset - 4, OffsetRelation.Segment))
                .WithExpectedValueOf(GEV.OFSMagicNumber)
                .WithReadWriteMetadata(Encoding.ASCII)
                .WithReadWriteMetadata<int>(GEV.OFSMagicNumber.Length, nameof(EMetadataNames.StringLength)); ;
            builder
                .WithCollection<ushort>(gev => gev.OFS, gev => gev.OFSDataOffset)
                .ReadInto((gev, data, l) =>
                {
                    gev.OFS = data.Select(x => x.Value).ToList();
                })
                .WithReadItemCountOf(gev => gev.OFSDataCount)
                .WithWriteOrderOf(200);

            builder
                .WithField(x => x.STRMagic, gev => (gev.STRDataOffset - 4, OffsetRelation.Segment))
                .WithExpectedValueOf(GEV.STRMagicNumber)
                .WithReadWriteMetadata(Encoding.ASCII)
                .WithReadWriteMetadata<int>(GEV.STRMagicNumber.Length, nameof(EMetadataNames.StringLength));
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
                .WithWriteOrderOf(200);

            builder
                .RegisterInDI(containerBuilder);
        }
        public static void SetupEVE(ContainerBuilder containerBuilder = null)
        {
            containerBuilder ??= new ContainerBuilder()
                .WithRequiredServices(useOptimizedServices: false)
                .WithHelpers()
                .WithPrimitiveMarshalers();


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
