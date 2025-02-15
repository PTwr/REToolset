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
                .As<IWriteMarshaler<Int32>>()
                .As<IReadMarshaler<ushort>>()
                .As<IWriteMarshaler<ushort>>()
                .As<IReadMarshaler<short>>()
                .As<IWriteMarshaler<short>>();
            containerBuilder.RegisterType<StringMarshaler>()
                .As<IReadMarshaler<string>>()
                .As<IWriteMarshaler<string>>();

            new ObjectBuilder<XBFFile.XBFTreeNode>()
                //TODO overload for collection read metadata which would work on temp list to get item Id
                .WithReadMetadata((node) => new IDebugInfoMetadata.DebugInfoMetadata($"XBF Tree Node #{node.Parent.TreeStructure.IndexOf(node)}"))
                .WithWriteMetadata((node) => new IDebugInfoMetadata.DebugInfoMetadata($"XBF Tree Node #{node.Parent.TreeStructure.IndexOf(node)}"))
                //TODO generic activators to take care of parent type casting?
                .WithDefaultActivator((parent) => new XBFFile.XBFTreeNode((XBFFile)parent))
                .WithReadByteLengthOf((node) => 4)
                .WithWriteByteLengthOf((node) => 4)

                .WithFieldOf<short>()
                .AtOffset((node) => (0, OffsetRelation.Segment))
                .ReadInto((node, x) => node.NameOrAttributeId = x)
                .WriteFrom((node) => node.NameOrAttributeId)
                .Done()

                .WithFieldOf<ushort>()
                .AtOffset((node) => (2, OffsetRelation.Segment))
                .ReadInto((node, x) => node.ValueId = x)
                .WriteFrom((node) => node.ValueId)
                .Done()

                .RegisterInDI(containerBuilder);

            new ObjectBuilder<XBFFile>()
                .WithReadMetadata((node) => new IDebugInfoMetadata.DebugInfoMetadata("XBF File"))
                .WithWriteMetadata((node) => new IDebugInfoMetadata.DebugInfoMetadata("XBF File"))
                .WithDefaultActivator((parent) => parent is U8FileNode fileNode ? new XBFFile(fileNode) : new XBFFile())

                .WithFieldOf<int>()
                .AtOffset((xbf) => (0, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.Magic1 = x)
                .WriteFrom((xbf) => xbf.Magic1)
                .WithAfterReadValidator((xbf) => xbf.Magic1 == XBFFile.MagicNumber1)
                .WithBeforeWriteValidator((xbf) => xbf.Magic1 == XBFFile.MagicNumber1)
                .Done()

                .WithFieldOf<int>()
                .AtOffset((xbf) => (4, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.Magic2 = x)
                .WriteFrom((xbf) => xbf.Magic2)
                .WithAfterReadValidator((xbf) => xbf.Magic2 == XBFFile.MagicNumber2)
                .WithBeforeWriteValidator((xbf) => xbf.Magic2 == XBFFile.MagicNumber2)
                .Done()

                .WithFieldOf<int>()
                .AtOffset((xbf) => (8, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.TreeStructureOffset = x)
                .WriteFrom((xbf) => xbf.TreeStructureOffset)
                .WithAfterReadValidator((xbf) => xbf.TreeStructureOffset == XBFFile.ExpectedTreeStructureOffset)
                .WithBeforeWriteValidator((xbf) => xbf.TreeStructureOffset == XBFFile.ExpectedTreeStructureOffset)
                .Done()

                .WithFieldOf<int>()
                .AtOffset((xbf) => (12, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.TreeStructureCount = x)
                .WriteFrom((xbf) => xbf.TreeStructure.Count)
                .Done()

                .WithFieldOf<int>()
                .AtOffset((xbf) => (16, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.TagListOffset = x)
                .WriteFrom((xbf) => XBFFile.ExpectedTreeStructureOffset + xbf.TreeStructure.Count * 4)
                //TODO .Read/WriteAfterFieldMarshaler(string precedingFieldMarshalerName) ? 
                .WithWriteOrderOf((xbf) => 10) //after tree structure
                .Done()

                .WithFieldOf<int>()
                .AtOffset((xbf) => (20, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.TagListCount = x)
                .WriteFrom((xbf) => xbf.TagList.Count)
                .Done()

                .WithFieldOf<int>()
                .AtOffset((xbf) => (24, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.AttributeListOffset = x)
                .WriteFrom((xbf) => xbf.AttributeListOffset)
                .WithWriteOrderOf((xbf) => 20) //after tag list
                .Done()

                .WithFieldOf<int>()
                .AtOffset((xbf) => (28, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.AttributeListCount = x)
                .WriteFrom((xbf) => xbf.AttributeList.Count)
                .Done()

                .WithFieldOf<int>()
                .AtOffset((xbf) => (32, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.ValueListOffset = x)
                .WriteFrom((xbf) => xbf.ValueListOffset)
                .WithWriteOrderOf((xbf) => 20) //after attribute list
                .Done()

                .WithFieldOf<int>()
                .AtOffset((xbf) => (36, OffsetRelation.Segment))
                .ReadInto((xbf, x) => xbf.ValueListCount = x)
                .WriteFrom((xbf) => xbf.ValueList.Count)
                .Done()

                .WithCollectionOf<XBFFile.XBFTreeNode>()
                .WithWriteOrderOf((xbf) => 1) //before list offsets
                .AtOffset((xbf) => (xbf.TreeStructureOffset, OffsetRelation.Segment))
                .WithReadItemCountOf((xbf) => xbf.TreeStructureCount)
                .WriteFrom((xbf) => xbf.TreeStructure)
                .ReadInto((xbf, data) =>
                {
                    xbf.TreeStructure = data.data.Select(x => x.Value).Where(x => x is not null).ToList();
                })
                .WithOnAfterWrite((xbf, bytesWrote) =>
                    xbf.TagListOffset = XBFFile.ExpectedTreeStructureOffset + bytesWrote
                )                
                .Done()

                .WithCollectionOf<string>()
                .WithReadMetadata((node) => new IDebugInfoMetadata.DebugInfoMetadata("TagList"))
                .WithWriteMetadata((node) => new IDebugInfoMetadata.DebugInfoMetadata("TagList"))
                .WithWriteOrderOf((xbf) => 11) //after taglist offset
                .AtOffset((xbf) => (xbf.TagListOffset, OffsetRelation.Segment))
                .WithReadItemCountOf((xbf) => xbf.TagListCount)
                //TODO set inheritable meta on XbfFile? Or before it as it depends on external info?
                .WithReadMetadata((xbf) => xbf.EncodingOverride ?? BinaryStringHelper.UTF8)
                .WithReadMetadata((xbf) => StringLengthStyle.NullTerminator)
                .WithWriteMetadata((xbf) => xbf.EncodingOverride ?? BinaryStringHelper.UTF8)
                .WithWriteMetadata((xbf) => StringLengthStyle.NullTerminator)
                .WriteFrom((xbf) => xbf.TagList)
                .ReadInto((xbf, data) =>
                {
                    xbf.TagList = new DistinctList<string>(data.data.Select(x => x.Value).Where(x => x is not null).ToList());
                })
                .WithOnAfterWrite((xbf, bytesWrote) =>
                    xbf.AttributeListOffset = xbf.TagListOffset + bytesWrote
                )
                .Done()

                .WithCollectionOf<string>()
                .WithReadMetadata((node) => new IDebugInfoMetadata.DebugInfoMetadata("AttributeList"))
                .WithWriteMetadata((node) => new IDebugInfoMetadata.DebugInfoMetadata("AttributeList"))
                .WithWriteOrderOf((xbf) => 21) //after attributelist offset
                .AtOffset((xbf) => (xbf.AttributeListOffset, OffsetRelation.Segment))
                .WithReadItemCountOf((xbf) => xbf.AttributeListCount)
                .WithReadMetadata((xbf) => xbf.EncodingOverride ?? BinaryStringHelper.UTF8)
                .WithReadMetadata((xbf) => StringLengthStyle.NullTerminator)
                .WithWriteMetadata((xbf) => xbf.EncodingOverride ?? BinaryStringHelper.UTF8)
                .WithWriteMetadata((xbf) => StringLengthStyle.NullTerminator)
                .WriteFrom((xbf) => xbf.AttributeList)
                .ReadInto((xbf, data) =>
                {
                    xbf.AttributeList = new DistinctList<string>(data.data.Select(x => x.Value).Where(x => x is not null).ToList());
                })
                .WithOnAfterWrite((xbf, bytesWrote) =>
                    xbf.ValueListOffset = xbf.AttributeListOffset + bytesWrote
                )
                .Done()

                .WithCollectionOf<string>()
                .WithReadMetadata((node) => new IDebugInfoMetadata.DebugInfoMetadata("ValueList"))
                .WithWriteMetadata((node) => new IDebugInfoMetadata.DebugInfoMetadata("ValueList"))
                .WithWriteOrderOf((xbf) => 11) //after taglist offset
                .AtOffset((xbf) => (xbf.ValueListOffset, OffsetRelation.Segment))
                .WithReadItemCountOf((xbf) => xbf.ValueListCount)
                .WithReadMetadata((xbf) => xbf.EncodingOverride ?? BinaryStringHelper.UTF8)
                .WithReadMetadata((xbf) => StringLengthStyle.NullTerminator)
                .WithWriteMetadata((xbf) => xbf.EncodingOverride ?? BinaryStringHelper.UTF8)
                .WithWriteMetadata((xbf) => StringLengthStyle.NullTerminator)
                .WriteFrom((xbf) => xbf.ValueList)
                .ReadInto((xbf, data) =>
                {
                    xbf.ValueList = new DistinctList<string>(data.data.Select(x => x.Value).Where(x => x is not null).ToList());
                })
                .Done()

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

            Assert.NotNull(xbf.TreeStructure);
            Assert.NotEmpty(xbf.TreeStructure);
            Assert.Equal(xbf.TreeStructureCount, xbf.TreeStructure.Count);

            var s = xbf.ToXDocument().ToString();

            return;
        }

        [Fact]
        public void ReadWriteLoop()
        {
            var container = Setup();

            var bytes = File.ReadAllBytes(ResultParamXbfPath);

            IDataBuffer dataBuffer = new DefaultDataBuffer(bytes, false);
            IOffsetStack offsetStack = new DefaultOffsetStack();
            IMarshalingMetadata metadata = new DefaultMarshalingMetadata();

            var xbf = ReadHelper.Read<XBFFile>(container.Resolve<IMarshalerStore>(), null, dataBuffer, metadata, offsetStack, out _);

            var newBuffer = new DefaultDataBuffer([], true);
            WriteHelper.Write<XBFFile>(container.Resolve<IMarshalerStore>(), xbf, newBuffer, new DefaultMarshalingMetadata(), new DefaultOffsetStack(), out var bytesWrote);

            var newbytes = newBuffer.AsSpan().ToArray();

            Assert.Equal(bytes, newbytes);
        }
    }
}
