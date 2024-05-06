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

            //files have relation set to Absolute to simulate nested U8 archives
            mapEntryRaw.WithField(i => i.Data).AtOffset(0).RelativeTo(Common.OffsetRelation.Absolute);

            mapEntryDirectory.WithField(i => i.Mask).AtOffset(0).RelativeTo(Common.OffsetRelation.Absolute).WithExpectedValueOf(0x01010101);
            mapEntryDirectory.WithField(i => i.EntryCount).AtOffset(4).RelativeTo(Common.OffsetRelation.Absolute);
            mapEntryDirectory.WithCollectionOf(i => i.Entries).AtOffset(5).RelativeTo(Common.OffsetRelation.Absolute)
                .WithCountOf(i => i.EntryCount);

            mapEntryA.WithField(i => i.Mask).AtOffset(0).RelativeTo(Common.OffsetRelation.Absolute).WithExpectedValueOf(0x02020202);
            mapEntryA.WithField(i => i.A).AtOffset(4).RelativeTo(Common.OffsetRelation.Absolute);
            mapEntryA.WithField(i => i.B).AtOffset(5).RelativeTo(Common.OffsetRelation.Absolute);
            mapEntryA.WithField(i => i.C).AtOffset(6).RelativeTo(Common.OffsetRelation.Absolute);
            mapEntryA.WithField(i => i.D).AtOffset(7).RelativeTo(Common.OffsetRelation.Absolute);

            mapEntryB.WithField(i => i.Mask).AtOffset(0).RelativeTo(Common.OffsetRelation.Absolute).WithExpectedValueOf(0x03030303);
            mapEntryB.WithField(i => i.S).AtOffset(4).RelativeTo(Common.OffsetRelation.Absolute);

            //in-segment data
            mapDescriptor.WithField(i => i.Offset).AtOffset(0).RelativeTo(Common.OffsetRelation.Segment);
            mapDescriptor.WithField(i => i.Length).AtOffset(1).RelativeTo(Common.OffsetRelation.Segment);
            //out-of-segment storage, like in U8
            mapDescriptor.WithField(i => i.Entry).AtOffset(i => i.Offset).RelativeTo(Common.OffsetRelation.Absolute);
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

        }
    }
}
