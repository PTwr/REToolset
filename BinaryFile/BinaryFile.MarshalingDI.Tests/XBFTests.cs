using BinaryFile.Formats.Nintendo.R79JAF;
using BinaryFile.MarshalingDI.Context;
using BinaryFile.MarshalingDI.DAL;
using BinaryFile.MarshalingDI.ComplexMarshaling;
using BinaryFile.MarshalingDI.Marshaling.Activating;
using BinaryFile.MarshalingDI.Marshaling.Reading;
using BinaryFile.MarshalingDI.Marshaling.Writing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using BinaryFile.MarshalingDI.Marshaling.Collection;
using BinaryFile.MarshalingDI.Marshaling.Complex;
using BinaryFile.MarshalingDI.PrimitiveMarshaling;
using BinaryFile.Formats.Nintendo;
using BinaryFile.MarshalingDI.Marshaling.Helpers;

namespace BinaryFile.MarshalingDI.Tests
{
    public class XBFTests
    {
        IContainer Setup()
        {
            ContainerBuilder containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterType<DefaultMarshalerStore>()
                .As<IMarshalerStore>();
            containerBuilder.RegisterType<DefaultCollectionMarshaler>()
                .As<DefaultCollectionMarshaler>();
            containerBuilder.RegisterType<IntegerMarshaler>()
                .As<IReadMarshaler<byte>>()
                .As<IWriteMarshaler<byte>>()
                .As<IReadMarshaler<Int32>>()
                .As<IWriteMarshaler<Int32>>();

            new ObjectMarshaler<XBFFile>.Builder()
                .WithDefaultActivator((parent) => parent is U8FileNode fileNode ? new XBFFile(fileNode) : new XBFFile())

                .WithField<int>()
                .AtOffset((xbf) => (0, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.Magic1 = x)
                .WriteFrom((xbf) => xbf.Magic1)
                .WithAfterReadValidator((xbf) => xbf.Magic1 == XBFFile.MagicNumber1)
                .WithBeforeWriteValidator((xbf) => xbf.Magic1 == XBFFile.MagicNumber1)
                .Done()

                .WithField<int>()
                .AtOffset((xbf) => (4, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.Magic2 = x)
                .WriteFrom((xbf) => xbf.Magic2)
                .WithAfterReadValidator((xbf) => xbf.Magic2 == XBFFile.MagicNumber2)
                .WithBeforeWriteValidator((xbf) => xbf.Magic2 == XBFFile.MagicNumber2)
                .Done()

                .WithField<int>()
                .AtOffset((xbf) => (8, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.TreeStructureOffset = x)
                .WriteFrom((xbf) => xbf.TreeStructureOffset)
                .WithAfterReadValidator((xbf) => xbf.TreeStructureOffset == XBFFile.ExpectedTreeStructureOffset)
                .WithBeforeWriteValidator((xbf) => xbf.TreeStructureOffset == XBFFile.ExpectedTreeStructureOffset)
                .Done()

                .WithField<int>()
                .AtOffset((xbf) => (12, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.TreeStructureCount = x)
                .WriteFrom((xbf) => xbf.TreeStructure.Count)
                .Done()

                .WithField<int>()
                .AtOffset((xbf) => (16, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.TagListOffset = x)
                .WriteFrom((xbf) => XBFFile.ExpectedTreeStructureOffset + xbf.TreeStructure.Count * 4)
                .WithWriteOrderOf((xbf) => 10) //after tree structure
                .Done()

                .WithField<int>()
                .AtOffset((xbf) => (20, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.TagListCount = x)
                .WriteFrom((xbf) => xbf.TagList.Count)
                .Done()

                .WithField<int>()
                .AtOffset((xbf) => (24, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.AttributeListOffset = x)
                .WriteFrom((xbf) => xbf.AttributeListOffset)
                .WithWriteOrderOf((xbf) => 20) //after tag list
                .Done()

                .WithField<int>()
                .AtOffset((xbf) => (28, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.AttributeListCount = x)
                .WriteFrom((xbf) => xbf.AttributeList.Count)
                .Done()

                .WithField<int>()
                .AtOffset((xbf) => (32, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.ValueListOffset = x)
                .WriteFrom((xbf) => xbf.ValueListOffset)
                .WithWriteOrderOf((xbf) => 20) //after attribute list
                .Done()

                .WithField<int>()
                .AtOffset((xbf) => (36, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.ValueListCount = x)
                .WriteFrom((xbf) => xbf.ValueList.Count)
                .Done()

                //TODO collections

                .RegisterInDI(containerBuilder);


            return containerBuilder.Build();
        }
        //TODO generate test samples once serialization is complete :)
        const string ResultParamXbfPath = @"C:\G\Wii\R79JAF_clean\DATA\files\parameter\result_param.xbf";

        [Fact]
        public void XBFHeaderRead()
        {
            var container = Setup();

            var bytes = File.ReadAllBytes(ResultParamXbfPath);

            IDataBuffer dataBuffer = new DefaultDataBuffer(bytes, false);
            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalingMetadata metadata = new DefaultMarshalingMetadata();

            var xbf = ReadHelper.Read<XBFFile>(container.Resolve<IMarshalerStore>(), null, dataBuffer, metadata, offsetStack, out _);

            Assert.NotNull(xbf);

            Assert.Equal(0x0028, xbf.TreeStructureOffset);
            Assert.Equal(0x0DF9, xbf.TreeStructureCount);

            Assert.Equal(0x380C, xbf.TagListOffset);
            Assert.Equal(0x0014, xbf.TagListCount);

            Assert.Equal(0x38B7, xbf.AttributeListOffset);
            Assert.Equal(0x0003, xbf.AttributeListCount);

            Assert.Equal(0x38C2, xbf.ValueListOffset);
            Assert.Equal(0x00AF, xbf.ValueListCount);

            return;
        }
    }
}
