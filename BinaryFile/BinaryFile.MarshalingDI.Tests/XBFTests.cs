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
using BinaryDataHelper;
using BinaryFile.MarshalingDI.DI;
using BinaryFile.MarshalingDI.Files;

namespace BinaryFile.MarshalingDI.Tests
{
    public class XBFTests
    {
        IContainer Setup(bool withBodyMarshaling)
        {
            ContainerBuilder containerBuilder = new ContainerBuilder();

            containerBuilder
                .WithRequiredServices(useOptimizedServices: false)
                .WithHelpers()
                .WithPrimitiveMarshalers();

            new ObjectBuilder<XBFFile.XBFTreeNode>()
                //TODO overload for collection read metadata which would work on temp list to get item Id
                .WithDebugInfo(node => $"XBF Tree Node #{node.Parent.TreeStructure.IndexOf(node)}")
                //TODO generic activators to take care of parent type casting?
                .WithDefaultActivator<XBFFile>((parent) => new XBFFile.XBFTreeNode(parent))
                .WithReadByteLengthOf((node) => 4)
                .WithWriteByteLengthOf((node) => 4)

                .WithFieldOf<short>()
                .AtOffset((node) => (0, OffsetRelation.Segment))
                .ReadInto((node, x) => node.NameOrAttributeId = x)
                .WriteFrom((node) => node.NameOrAttributeId)
                .Done(containerBuilder)

                .WithFieldOf<ushort>()
                .AtOffset((node) => (2, OffsetRelation.Segment))
                .ReadInto((node, x) => node.ValueId = x)
                .WriteFrom((node) => node.ValueId)
                .Done(containerBuilder)

                .RegisterInDI(containerBuilder);

            var marshalerBuilder = new ObjectBuilder<XBFFile>()
                .WithDebugInfo(xbf => $"XBF File")
                .WithMagicPatternOf(XBFFile.MagicPattern).AlsoActivateFor<IFile>()
                .WithDefaultActivator((parent) => parent is U8FileNode fileNode ? new XBFFile(fileNode) : new XBFFile())

                //BigEndian - human readable hexes, Little Endian - Intels annoying memory layout
                .WithReadWriteMetadata((f, s) => EMarshalingEndianness.BigEndian, true, null, int.MaxValue)

                //XBF has no nested files, thus string metas can be set as "infinite"
                //TODO explicit setting for infinite meta instead of int.MaxValue hack?
                .WithReadWriteMetadata((f, s) => XBFFile.GetEncodingForFileName(f.GetFileName()), true, null, int.MaxValue)
                //STR section strings are null terminated AND aligned to 32bit, same with strings embedeed in EVE?
                //TODO alignmenet/padding
                .WithReadWriteMetadata((f, s) => EStringLengthStyle.NullTerminator, true, null, int.MaxValue)

                .WithFieldOf<int>()
                .WithDebugInfo((xbf) => $"Magic number 1 ({XBFFile.MagicNumber1:0x})")
                .AtOffset((xbf) => (0, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.Magic1 = x)
                .WriteFrom((xbf) => xbf.Magic1)
                .WithAfterReadValidator((c) => c.Resolve<IFeatureSetStack>().GetParent<XBFFile>().Magic1 == XBFFile.MagicNumber1)
                .WithBeforeWriteValidator((c) => c.Resolve<IFeatureSetStack>().GetParent<XBFFile>().Magic1 == XBFFile.MagicNumber1)
                .Done(containerBuilder)

                .WithFieldOf<int>()
                .WithDebugInfo((xbf) => $"Magic number 2 ({XBFFile.MagicNumber2:0x})")
                .AtOffset((xbf) => (4, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.Magic2 = x)
                .WriteFrom((xbf) => xbf.Magic2)
                .WithAfterReadValidator((c) => c.Resolve<IFeatureSetStack>().GetParent<XBFFile>().Magic2 == XBFFile.MagicNumber2)
                .WithBeforeWriteValidator((c) => c.Resolve<IFeatureSetStack>().GetParent<XBFFile>().Magic2 == XBFFile.MagicNumber2)
                .Done(containerBuilder)

                .WithFieldOf<int>()
                .WithDebugInfo((xbf) => "Tree Structure Offset")
                .AtOffset((xbf) => (8, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.TreeStructureOffset = x)
                .WriteFrom((xbf) => xbf.TreeStructureOffset)
                .WithAfterReadValidator((c) => c.Resolve<IFeatureSetStack>().GetParent<XBFFile>().TreeStructureOffset == XBFFile.ExpectedTreeStructureOffset)
                .WithBeforeWriteValidator((c) => c.Resolve<IFeatureSetStack>().GetParent<XBFFile>().TreeStructureOffset == XBFFile.ExpectedTreeStructureOffset)
                .Done(containerBuilder)

                .WithFieldOf<int>()
                .WithDebugInfo((xbf) => "Tree Structure Count")
                .AtOffset((xbf) => (12, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.TreeStructureCount = x)
                .WriteFrom((xbf) => xbf.TreeStructure.Count)
                .Done(containerBuilder)

                .WithFieldOf<int>()
                .WithDebugInfo((xbf) => "Tag List Offset")
                .AtOffset((xbf) => (16, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.TagListOffset = x)
                .WriteFrom((xbf) => XBFFile.ExpectedTreeStructureOffset + xbf.TreeStructure.Count * 4)
                //TODO .Read/WriteAfterFieldMarshaler(string precedingFieldMarshalerName) ? 
                .WithWriteOrderOf((xbf) => 10) //after tree structure
                .Done(containerBuilder)

                .WithFieldOf<int>()
                .WithDebugInfo((xbf) => "Tag List Count")
                .AtOffset((xbf) => (20, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.TagListCount = x)
                .WriteFrom((xbf) => xbf.TagList.Count)
                .Done(containerBuilder)

                .WithFieldOf<int>()
                .WithDebugInfo((xbf) => "Attribute List Offset")
                .AtOffset((xbf) => (24, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.AttributeListOffset = x)
                .WriteFrom((xbf) => xbf.AttributeListOffset)
                .WithWriteOrderOf((xbf) => 20) //after tag list
                .Done(containerBuilder)

                .WithFieldOf<int>()
                .WithDebugInfo((xbf) => "Attribute List Count")
                .AtOffset((xbf) => (28, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.AttributeListCount = x)
                .WriteFrom((xbf) => xbf.AttributeList.Count)
                .Done(containerBuilder)

                .WithFieldOf<int>()
                .WithDebugInfo((xbf) => "Value List Offset")
                .AtOffset((xbf) => (32, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.ValueListOffset = x)
                .WriteFrom((xbf) => xbf.ValueListOffset)
                .WithWriteOrderOf((xbf) => 20) //after attribute list
                .Done(containerBuilder)

                .WithFieldOf<int>()
                .WithDebugInfo((xbf) => "Value List Count")
                .AtOffset((xbf) => (36, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.ValueListCount = x)
                .WriteFrom((xbf) => xbf.ValueList.Count)
                .Done(containerBuilder);

            if (withBodyMarshaling)
            {
                marshalerBuilder = marshalerBuilder

                    .WithCollectionOf<XBFFile.XBFTreeNode>()
                    .WithDebugInfo((xbf) => "Tree Structure")
                    .WithWriteOrderOf((xbf) => 1) //before list offsets
                    .AtOffset((c) => (c.Resolve<IFeatureSetStack>().GetParent<XBFFile>().TreeStructureOffset, OffsetRelation.Segment))
                    .WithReadItemCountOf((xbf) => xbf.TreeStructureCount)
                    .WriteFrom((xbf) => xbf.TreeStructure)
                    .ReadInto((xbf, data) =>
                    {
                        xbf.TreeStructure = data.data.Select(x => x.Value).Where(x => x is not null).ToList();
                    })
                    .WithOnAfterWrite((c, bytesWrote) =>
                        c.Resolve<IFeatureSetStack>().GetParent<XBFFile>().TagListOffset = XBFFile.ExpectedTreeStructureOffset + bytesWrote
                    )
                    .Done(containerBuilder)

                    .WithCollectionOf<string>()
                    .WithDebugInfo((xbf) => "TagList")
                    .WithWriteOrderOf((xbf) => 11) //after taglist offset
                    .AtOffset((c) => (c.Resolve<IFeatureSetStack>().GetParent<XBFFile>().TagListOffset, OffsetRelation.Segment))
                    .WithReadItemCountOf((xbf) => xbf.TagListCount)
                    .WriteFrom((xbf) => xbf.TagList)
                    .ReadInto((xbf, data) =>
                    {
                        xbf.TagList = new DistinctList<string>(data.data.Select(x => x.Value).Where(x => x is not null).ToList());
                    })
                    //TODO helper/override/extension to autoresolve parent?
                    .WithOnAfterWrite((c, bytesWrote) =>
                    {
                        var xbf = c.Resolve<IFeatureSetStack>().GetParent<XBFFile>();
                        xbf.AttributeListOffset = xbf.TagListOffset + bytesWrote;
                    })
                    .Done(containerBuilder)

                    .WithCollectionOf<string>()
                    .WithDebugInfo((node) => "XBF Attribute List")
                    //TODO const value overloads
                    .WithWriteOrderOf((c) => 21) //after attributelist offset
                                                 //TODO cleanup mess between IContainer and TDeclaredType overloads
                    .AtOffset((c) => (c.GetParent<XBFFile>().AttributeListOffset, OffsetRelation.Segment))
                    .WithReadItemCountOf((xbf) => xbf.AttributeListCount)
                    .WriteFrom((xbf) => xbf.AttributeList)
                    .ReadInto((xbf, data) =>
                    {
                        xbf.AttributeList = new DistinctList<string>(data.data.Select(x => x.Value).Where(x => x is not null).ToList());
                    })
                    .WithOnAfterWrite((c, bytesWrote) =>
                    {
                        var xbf = c.GetParent<XBFFile>();
                        xbf.ValueListOffset = xbf.AttributeListOffset + bytesWrote;
                    })
                    .Done(containerBuilder)

                    .WithCollectionOf<string>()
                    .WithDebugInfo((node) => "Value List")
                    .WithWriteOrderOf((c) => 11) //after taglist offset
                    .AtOffset((c) => (c.GetParent<XBFFile>().ValueListOffset, OffsetRelation.Segment))
                    .WithReadItemCountOf((xbf) => xbf.ValueListCount)
                    .WriteFrom((xbf) => xbf.ValueList)
                    .ReadInto((xbf, data) =>
                    {
                        xbf.ValueList = new DistinctList<string>(data.data.Select(x => x.Value).Where(x => x is not null).ToList());
                    })
                    .Done(containerBuilder);
            }

            marshalerBuilder.RegisterInDI(containerBuilder);

            return containerBuilder.Build();
        }
        //TODO generate test samples once serialization is complete :)
        const string ResultParamXbfPath = @"C:\G\Wii\R79JAF_clean\DATA\files\parameter\result_param.xbf";

        [Fact]
        public void XBFDetectionAsIFile()
        {
            var container = Setup(false);

            byte[] bytesNotXBF = [1, 2, 3, 4, 5];
            var bytesXBF = File.ReadAllBytes(ResultParamXbfPath);

            container.Resolve<IDataBufferIO>().SetData(bytesNotXBF);

            var readHelper = container.Resolve<ReadHelper>();

            //Assert.Throws<InvalidOperationException>(() => readHelper.Read<XBFFile>(out _));

            var iFile = readHelper.Read<IFile>(out _);

            Assert.IsType<RawBinaryFile>(iFile);

            container.Resolve<IDataBufferIO>().SetData(bytesXBF);

            var iFileShouldBeXBF = readHelper.Read<IFile>(out _);
            Assert.IsType<XBFFile>(iFileShouldBeXBF);
        }

        [Fact]
        public void XBFHeaderRead()
        {
            var container = Setup(false);

            var bytes = File.ReadAllBytes(ResultParamXbfPath);

            container.Resolve<IDataBufferIO>().SetData(bytes);

            var readHelper = container.Resolve<ReadHelper>();

            var xbf = readHelper.Read<XBFFile>(out var bytesRead);

            Assert.NotNull(xbf);

            Assert.Equal(0x0028, xbf.TreeStructureOffset);
            Assert.Equal(0x0DF9, xbf.TreeStructureCount);

            Assert.Equal(0x380C, xbf.TagListOffset);
            Assert.Equal(0x0014, xbf.TagListCount);

            Assert.Equal(0x38B7, xbf.AttributeListOffset);
            Assert.Equal(0x0003, xbf.AttributeListCount);

            Assert.Equal(0x38C2, xbf.ValueListOffset);
            Assert.Equal(0x00AF, xbf.ValueListCount);

            var s = xbf.ToXDocument().ToString();

            return;
        }
        [Fact]
        public void XBFBodyRead()
        {
            var container = Setup(true);

            var bytes = File.ReadAllBytes(ResultParamXbfPath);

            container.Resolve<IDataBufferIO>().SetData(bytes);

            var readHelper = container.Resolve<ReadHelper>();

            var xbf = readHelper.Read<XBFFile>(out var bytesRead);

            Assert.NotNull(xbf);

            Assert.NotNull(xbf.TreeStructure);
            Assert.NotEmpty(xbf.TreeStructure);
            Assert.Equal(xbf.TreeStructureCount, xbf.TreeStructure.Count);

            var s = xbf.ToXDocument().ToString();

            return;
        }

        [Fact]
        public void ReadWriteLoop()
        {
            var container = Setup(true);

            var bytes = File.ReadAllBytes(ResultParamXbfPath);

            container.Resolve<IDataBufferIO>().SetData(bytes);

            var readHelper = container.Resolve<ReadHelper>();
            var xbf = readHelper.Read<XBFFile>(out var bytesRead);


            container.Resolve<IDataBufferIO>().Reset();
            container.Resolve<IDataBufferIO>().EnableResize();

            var writeHelper = container.Resolve<WriteHelper>();
            writeHelper.Write<XBFFile>(xbf, out var bytesWrote);

            var newbytes = container.Resolve<IDataBufferIO>().GetData();

            Assert.Equal(bytes, newbytes);
        }
    }
}
