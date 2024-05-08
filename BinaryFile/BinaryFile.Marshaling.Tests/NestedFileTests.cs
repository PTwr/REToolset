using BinaryDataHelper;
using BinaryFile.Marshaling.Activation;
using BinaryFile.Marshaling.Context;
using BinaryFile.Marshaling.MarshalingStore;
using BinaryFile.Marshaling.PrimitiveMarshaling;
using BinaryFile.Marshaling.TypeMarshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.Marshaling.Tests
{
    public class NestedFileTests
    {
        byte[] expected = [
            0x01, 0x01, 0x01, 0x01, //directory magic
            //TODO switch to 0x04 to test nested DirectoryEntry once nested absolute is fixed on Raw/A/B as it will go into stack overflow until its done
            0x04, //4 files
            0x0D, 0x05, //raw file
            0x0D+0x05, 0x08, //file A
            0x0D+0x05+0x08, 0x08, //file B
            0x0D+0x05+0x08+0x08, 0x09+0x08+0x08, //nested directory
            //start of data section
            //raw file
            0x01, 0x02, 0x03, 0x04, 0x05,
            //file A
            0x02, 0x02, 0x02, 0x02, //mask
            0x10, 0x11, 0x12, 0x13, //ABCD
            //file B
            0x03, 0x03, 0x03, 0x03, //mask
            0x41, 0x42, 0x43, 0x44, //S = "ABCD"

            //nested directory
            0x01, 0x01, 0x01, 0x01, //magic
            0x02, //2 files
            0x09, 0x08,
            0x09+0x08, 0x08,
            //file A
            0x02, 0x02, 0x02, 0x02, //mask
            0x20, 0x21, 0x22, 0x23, //ABCD
            //file B
            0x03, 0x03, 0x03, 0x03, //mask
            0x45, 0x46, 0x47, 0x48, //S = "EFGH"
            ];

        public interface IEntry
        {

        }
        public class EntryDescriptor
        {
            public byte Offset { get; set; }
            public byte Length { get; set; }
            public IEntry Entry { get; set; }
        }
        public class RawFile : IEntry
        {
            public byte[] Data { get; set; }
        }
        public class DirectoryEntry : IEntry
        {
            public int Mask { get; set; } = 0x01010101;
            public byte EntryCount { get; set; }
            public List<EntryDescriptor> Entries { get; set; }
        }
        public class FileA : IEntry
        {
            public int Mask { get; set; } = 0x02020202;
            public byte A { get; set; }
            public byte B { get; set; }
            public byte C { get; set; }
            public byte D { get; set; }
        }
        public class FileB : IEntry
        {
            public int Mask { get; set; } = 0x03030303;
            public string S { get; set; }
        }

        private static IMarshalingContext Prep(out ITypeMarshaler<DirectoryEntry> m)
        {
            var store = new MarshalerStore();
            var rootCtx = new RootMarshalingContext(store);

            var mapDescriptor = new RootTypeMarshaler<EntryDescriptor>();
            var mapEntryInterface = new RootTypeMarshaler<IEntry>();

            var mapEntryRaw = mapEntryInterface.Derive<RawFile>();
            var mapEntryDirectory = mapEntryInterface.Derive<DirectoryEntry>();
            var mapEntryA = mapEntryInterface.Derive<FileA>();
            var mapEntryB = mapEntryInterface.Derive<FileB>();

            store.Register(mapDescriptor);
            store.Register(mapEntryInterface);

            //files have relation set to Absolute to simulate nested U8 archives
            mapEntryRaw.WithField(i => i.Data).AtOffset(0).RelativeTo(Common.OffsetRelation.Absolute);

            mapEntryDirectory.WithField(i => i.Mask).AtOffset(0).RelativeTo(Common.OffsetRelation.Absolute)
                .WithExpectedValueOf(0x01010101);
            mapEntryDirectory.WithField(i => i.EntryCount).AtOffset(4).RelativeTo(Common.OffsetRelation.Absolute);
            mapEntryDirectory.WithCollectionOf(i => i.Entries).AtOffset(5).RelativeTo(Common.OffsetRelation.Absolute)
                .WithCountOf(i => i.EntryCount);

            mapEntryA.WithField(i => i.Mask).AtOffset(0).RelativeTo(Common.OffsetRelation.Absolute)
                .WithExpectedValueOf(0x02020202);
            mapEntryA.WithField(i => i.A).AtOffset(4).RelativeTo(Common.OffsetRelation.Absolute);
            mapEntryA.WithField(i => i.B).AtOffset(5).RelativeTo(Common.OffsetRelation.Absolute);
            mapEntryA.WithField(i => i.C).AtOffset(6).RelativeTo(Common.OffsetRelation.Absolute);
            mapEntryA.WithField(i => i.D).AtOffset(7).RelativeTo(Common.OffsetRelation.Absolute);

            mapEntryB.WithField(i => i.Mask).AtOffset(0).RelativeTo(Common.OffsetRelation.Absolute)
                .WithExpectedValueOf(0x03030303);
            mapEntryB.WithField(i => i.S).AtOffset(4).RelativeTo(Common.OffsetRelation.Absolute);

            //in-segment data
            mapDescriptor.WithField(i => i.Offset).AtOffset(0).RelativeTo(Common.OffsetRelation.Segment);
            mapDescriptor.WithField(i => i.Length).AtOffset(1).RelativeTo(Common.OffsetRelation.Segment);
            //out-of-segment storage, like in U8
            mapDescriptor.WithField(i => i.Entry).AtOffset(i => i.Offset).RelativeTo(Common.OffsetRelation.Absolute)
                .WithByteLengthOf(i => i.Length)
                .AsNestedFile();
            mapDescriptor.WithByteLengthOf(2);

            //fallback activation to equivalent of byte[]
            mapEntryInterface.WithCustomActivator(new CustomActivator<IEntry>((data, ctx) =>
            {
                return new RawFile();
            }, order: int.MaxValue));

            //In real usage additional deriveed Entries would register their own Activators, preferably solving conflicts through Magic patterns but controling Order is also an option to provide override
            mapEntryInterface.WithCustomActivator(new CustomActivator<IEntry>((data, ctx) =>
            {
                var span = ctx.ItemSlice(data).Span;
                if (span.StartsWith(0x01010101)) return new DirectoryEntry();
                if (span.StartsWith(0x02020202)) return new FileA();
                if (span.StartsWith(0x03030303)) return new FileB();
                return null;
            }, order: 0));

            //TODO modify FindMarshaler to be able to find derived implementations when concrete type is actually known prior to reading bytes
            m = store.FindMarshaler<DirectoryEntry>();

            Assert.NotNull(m);

            store.Register(new IntegerMarshaler());
            store.Register(new StringMarshaler());
            store.Register(new IntegerArrayMarshaler());

            return rootCtx;
        }


        [Fact]
        public void NestedObjectAbsoluteFields()
        {
            var ctx = Prep(out var m);

            var obj = m.Deserialize(null, null, expected.AsMemory(), ctx, out var l);

            Assert.IsType<RawFile>(obj.Entries[0].Entry);
            Assert.IsType<FileA>(obj.Entries[1].Entry);
            Assert.IsType<FileB>(obj.Entries[2].Entry);
            Assert.IsType<DirectoryEntry>(obj.Entries[3].Entry);

            var rawFile = obj.Entries[0].Entry as RawFile;
            Assert.NotNull(rawFile);
            Assert.Equal(5, rawFile.Data.Length);
            //TODO context switch when entering nested file
            Assert.Equal([0x01, 0x02, 0x03, 0x04, 0x05], rawFile.Data);

            var fileA = obj.Entries[1].Entry as FileA;
            Assert.NotNull(fileA);
            Assert.Equal(0x10, fileA.A);
            Assert.Equal(0x11, fileA.B);
            Assert.Equal(0x12, fileA.C);
            Assert.Equal(0x13, fileA.D);

            var fileB = obj.Entries[2].Entry as FileB;
            Assert.NotNull(fileB);
            Assert.Equal("ABCD", fileB.S);

            var directory = obj.Entries[3].Entry as DirectoryEntry;
            Assert.NotNull(directory);
            Assert.Equal(2, directory.EntryCount);

            var nestedA = directory.Entries[0].Entry as FileA;
            Assert.NotNull(nestedA);
            Assert.Equal(0x20, nestedA.A);
            Assert.Equal(0x21, nestedA.B);
            Assert.Equal(0x22, nestedA.C);
            Assert.Equal(0x23, nestedA.D);

            var nestedB = directory.Entries[1].Entry as FileB;
            Assert.NotNull(nestedB);
            Assert.Equal("EFGH", nestedB.S);
        }

        [Fact]
        public void NestedObjectReadWriteLoop()
        {
            var ctx = Prep(out var m);

            var obj = m.Deserialize(null, null, expected.AsMemory(), ctx, out var l);

            var buffer = new ByteBuffer(expected.Length * 2);
            m.Serialize(obj, buffer, ctx, out var l2);

            Assert.Equal(l, l2);
            var actual = buffer.GetData();

            Assert.Equal(expected, actual);
        }
    }
}
