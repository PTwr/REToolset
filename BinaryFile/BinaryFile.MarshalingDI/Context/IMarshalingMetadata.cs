using BinaryFile.MarshalingDI.DAL;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BinaryFile.MarshalingDI.Context
{
    //TODO inheritance of metadata? container could then set string encoding for all children, like XML root node. XBF could use it when encoding depends on file name (Externally controlled)
    /// <summary>
    /// optional Metadata not necessary for calculating Offset or Slicing
    /// </summary>
    public interface IMarshalingMetadata
    {
        void Add(object value);
        [return: NotNullIfNotNull(nameof(fallbackValue))]
        T? Get<T>(T? fallbackValue);
        IEnumerable<T> GetAll<T>();

        IMarshalingMetadata Fork();
        IMarshalingMetadata Concat(IMarshalingMetadata higherPriorityMetadata);
        IMarshalingMetadata Concat(IEnumerable<object> higherPriorityMetadata);

        IEnumerable<object> GetAll();
    }

    public static class Extensions
    {
        public static Encoding GetTextEncoding(this IMarshalingMetadata metadata)
            => metadata.Get(Encoding.ASCII);

        public static MarshalingEndianness GetEndianness(this IMarshalingMetadata metadata)
            => metadata.Get(MarshalingEndianness.BigEndian);
        public static bool IsLittleEndian(this IMarshalingMetadata metadata)
            => GetEndianness(metadata) == MarshalingEndianness.LittleEndian;

        public static StringLengthStyle GetStringLengthStyle(this IMarshalingMetadata metadata)
            => metadata.Get(StringLengthStyle.NullTerminator);

        public static bool HasCollectionCount(this IMarshalingMetadata metadata, out int count)
        {
            var meta = metadata.Get<ICollectionCountMetadata>(null);
            count = meta?.Count ?? 0;
            return meta != null;
        }

        public static bool HasStringLength(this IMarshalingMetadata metadata, out int length)
        {
            var meta = metadata.Get<IStringLengthMetadata>(null);
            length = meta?.Length ?? 0;
            return meta != null;
        }

        public static ICollectionReadWhileMetadata<TCollectionItem> GetCollectionReadWhile<TCollectionItem>(this IMarshalingMetadata metadata)
            => metadata.Get<ICollectionReadWhileMetadata<TCollectionItem>>(ICollectionReadWhileMetadata<TCollectionItem>.Fallback);

        public static string GetDebugInfo(this IMarshalingMetadata metadata)
            => string.Join(Environment.NewLine, metadata.GetAll<IDebugInfoMetadata>().Select(x => x.Info));
    }

    public enum MarshalingEndianness
    {
        BigEndian = 0,
        LittleEndian = 1,
    }
    public enum StringLengthStyle
    {
        /// <summary>
        /// C string
        /// </summary>
        NullTerminator = 0,
        /// <summary>
        /// UCSD string, first byte is length
        /// </summary>
        PascalString = 1,
        /// <summary>
        /// Length will be fetched from IStringLengthMetadata 
        /// </summary>
        FixedLength = 2,

        //is byte segment sypport required?
        /// <summary>
        /// To the end of byte segment
        /// </summary>
        //WholeSegment = 3,
    }
    public interface IStringLengthMetadata
    {
        int Length { get; }
        public class StringLengthMetadata(int length) : IStringLengthMetadata
        {
            public int Length { get; private set; } = length;
        }
    }
    public interface ICollectionCountMetadata
    {
        int Count { get; }
        public class CollectionCountMetadata(int count) : ICollectionCountMetadata
        {
            public int Count { get; private set; } = count;
        }
    }
    public interface ICollectionReadWhileMetadata<TCollectionItem>
    {
        bool ReadWhile(List<(int, TCollectionItem?)> currentCollection, IDataBuffer data, IMarshalingMetadata meta, IOffsetStack stack);

        public static CollectionReadWhileMetadata Fallback = new CollectionReadWhileMetadata();
        public class CollectionReadWhileMetadata : ICollectionReadWhileMetadata<TCollectionItem>
        {
            Func<List<(int, TCollectionItem?)>, IDataBuffer, IMarshalingMetadata, IOffsetStack, bool> readWhile;

            public CollectionReadWhileMetadata(Func<List<(int, TCollectionItem?)>, IDataBuffer, IMarshalingMetadata, IOffsetStack, bool> readWhile)
            {
                this.readWhile = readWhile;
            }

            internal CollectionReadWhileMetadata()
            {
                this.readWhile = (c, d, m, s) => true;
            }

            public bool ReadWhile(List<(int, TCollectionItem?)> currentCollection, IDataBuffer data, IMarshalingMetadata meta, IOffsetStack stack)
                => readWhile(currentCollection, data, meta, stack);
        }
    }
    public interface IDebugInfoMetadata
    {
        string Info { get; }

        public static DebufInfoMetadata Fallback = new DebufInfoMetadata();
        public class DebufInfoMetadata(string info = "") : IDebugInfoMetadata
        {
            public string Info { get; private set; } = info;
        }
    }

    public class DefaultMarshalingMetadata : IMarshalingMetadata
    {
        [return: NotNullIfNotNull(nameof(fallbackValue))]
        public T? Get<T>(T? fallbackValue)
            => GetAll<T>().DefaultIfEmpty(fallbackValue).Last();

        public IEnumerable<T> GetAll<T>()
            => metadata.OfType<T>();

        private List<object> metadata;
        public void Add(object value)
            => metadata.Add(value);

        public DefaultMarshalingMetadata()
        {
            metadata = new List<object>();
        }

        private DefaultMarshalingMetadata(DefaultMarshalingMetadata metadataToCopy)
        {
            this.metadata = metadataToCopy.metadata.ToList();
        }
        private DefaultMarshalingMetadata(IEnumerable<object> metadata)
        {
            this.metadata = metadata.ToList();
        }

        public IMarshalingMetadata Fork()
            => new DefaultMarshalingMetadata(this);

        public IEnumerable<object> GetAll()
            => metadata.AsReadOnly();

        public IMarshalingMetadata Concat(IMarshalingMetadata higherPriorityMetadata)
            => this.Concat(higherPriorityMetadata.GetAll());
        public IMarshalingMetadata Concat(IEnumerable<object> higherPriorityMetadata)
        {
            var joinedMeta = this.GetAll().Concat(higherPriorityMetadata);
            var forked = new DefaultMarshalingMetadata(joinedMeta);
            return forked;
        }
    }
}
