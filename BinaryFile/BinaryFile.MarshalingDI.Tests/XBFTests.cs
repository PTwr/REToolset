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
                .WithDebugInfo(node => $"XBF Tree Node #{node.Parent.TreeStructure.IndexOf(node)}")
                .WithDefaultActivator<XBFFile>((parent) => new XBFFile.XBFTreeNode(parent))
                .WithByteLengthOf(4)

                .WithField(node => node.NameOrAttributeId, offset: 0)
            //TODO store containerBuilder and fieldBuilders in ObjectBuilder, chain WithFieldOf from FieldBuilder?
                .Done(containerBuilder)

                .WithField(node => node.ValueId, 2)
                .Done(containerBuilder)

                .RegisterInDI(containerBuilder);

            var marshalerBuilder = new ObjectBuilder<XBFFile>()
                .WithDebugInfo(xbf => $"XBF File")
                .WithMagicPatternOf(XBFFile.MagicPattern)
                .AlsoActivateFor<IFile>()
                //TODO multiple Activators for different parents
                .WithDefaultActivator((parent) => parent is U8FileNode fileNode ? new XBFFile(fileNode) : new XBFFile())

                //BigEndian - human readable hexes, Little Endian - Intels annoying memory layout
                .InLittleEndian()

                //XBF has no nested files, thus metas can be set as "infinite"
                //TODO explicit setting for infinite meta instead of int.MaxValue hack?
                .WithReadWriteMetadata((f, s) => XBFFile.GetEncodingForFileName(f.GetFileName()), true, null, int.MaxValue)
                //TODO alignmenet/padding (for GEV/STR?)
                .WithReadWriteMetadata((f, s) => EStringLengthStyle.NullTerminator, true, null, int.MaxValue)

                .WithField(xbf => xbf.Magic1, 0)
                .WithDebugInfo($"Magic number 1 ({XBFFile.MagicNumber1:0x})")
                .WithExpectedValueOf(XBFFile.MagicNumber1)
                .Done(containerBuilder)

                .WithField(xbf => xbf.Magic2, 4)
                .WithDebugInfo($"Magic number 2 ({XBFFile.MagicNumber2:0x})")
                .WithExpectedValueOf(XBFFile.MagicNumber2)
                .Done(containerBuilder)

                .WithField(xbf => xbf.TreeStructureOffset, 8)
                .WithDebugInfo("Tree Structure Offset")
                .WithExpectedValueOf(XBFFile.ExpectedTreeStructureOffset)
                .Done(containerBuilder)

                .WithField(xbf => xbf.TreeStructureCount, 12)
                .WithDebugInfo("Tree Structure Count")
                .Done(containerBuilder)

                .WithField(xbf => xbf.TagListOffset, 16)
                .WithDebugInfo("Tag List Offset")
                //override autoprop to calculate value before writing
                .WriteFrom((xbf) => XBFFile.ExpectedTreeStructureOffset + xbf.TreeStructure.Count * 4)
                //TODO .Read/WriteAfterFieldMarshaler(string precedingFieldMarshalerName) ? Calculate names through lambdas by default?
                .WithWriteOrderOf(10) //after tree structure
                .Done(containerBuilder)

                .WithField(xbf => xbf.TagListCount, 20)
                .WithDebugInfo("Tag List Count")
                .WriteFrom((xbf) => xbf.TagList.Count)
                .Done(containerBuilder)

                .WithField(xbf => xbf.AttributeListOffset, 24)
                .WithDebugInfo("Attribute List Offset")
                .WithWriteOrderOf(20) //after tag list
                .Done(containerBuilder)

                .WithField(xbf => xbf.AttributeListCount, 28)
                .WithDebugInfo("Attribute List Count")
                .Done(containerBuilder)

                .WithField(xbf => xbf.ValueListOffset, 32)
                .WithDebugInfo("Value List Offset")
                .WithWriteOrderOf(20) //after attribute list
                .Done(containerBuilder)

                .WithField(xbf => xbf.ValueListCount, 36)
                .WithDebugInfo("Value List Count")
                .WriteFrom((xbf) => xbf.ValueList.Count)
                .Done(containerBuilder);

            if (withBodyMarshaling)
            {
                marshalerBuilder = marshalerBuilder

                    .WithCollection(xbf => xbf.TreeStructure, xbf => xbf.TreeStructureOffset)
                    .WithDebugInfo("Tree Structure")
                    .WithWriteOrderOf(1) //before list offsets
                    .WithReadItemCountOf((xbf) => xbf.TreeStructureCount)
                    .WithOnAfterWrite((c, bytesWrote) =>
                        c.Resolve<IFeatureSetStack>().GetParent<XBFFile>().TagListOffset = XBFFile.ExpectedTreeStructureOffset + bytesWrote
                    )
                    .Done(containerBuilder)

                    .WithCollection(xbf => xbf.TagList, xbf => xbf.TagListOffset)
                    .WithDebugInfo("TagList")
                    .WithWriteOrderOf(11) //after taglist offset
                    .WithReadItemCountOf((xbf) => xbf.TagListCount)
                    //TODO helper/override/extension to autoresolve parent?
                    .WithOnAfterWrite((c, bytesWrote) =>
                    {
                        var xbf = c.Resolve<IFeatureSetStack>().GetParent<XBFFile>();
                        xbf.AttributeListOffset = xbf.TagListOffset + bytesWrote;
                    })
                    .Done(containerBuilder)

                    .WithCollection(xbf => xbf.AttributeList, xbf => xbf.AttributeListOffset)
                    .WithDebugInfo("XBF Attribute List")
                    //TODO const value overloads
                    .WithWriteOrderOf(21) //after attributelist offset
                    .WithReadItemCountOf((xbf) => xbf.AttributeListCount)
                    .WithOnAfterWrite((c, bytesWrote) =>
                    {
                        var xbf = c.GetParent<XBFFile>();
                        xbf.ValueListOffset = xbf.AttributeListOffset + bytesWrote;
                    })
                    .Done(containerBuilder)

                    .WithCollection(xbf => xbf.ValueList, xbf => xbf.ValueListOffset)
                    .WithDebugInfo("Value List")
                    .WithWriteOrderOf(11) //after taglist offset
                    .WithReadItemCountOf((xbf) => xbf.ValueListCount)
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
