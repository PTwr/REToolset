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
    }

    public static class Extensions
    {
        public static Encoding GetEncoding(this IMarshalingMetadata metadata)
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

        public static ICollectionReadWhileMetadata<TCollectionItem> GetCollectionReadWhile<TCollectionItem>(this IMarshalingMetadata metadata)
            => metadata.Get<ICollectionReadWhileMetadata<TCollectionItem>>(ICollectionReadWhileMetadata<TCollectionItem>.FallbackSingleton);

        public static string GetDebugInfo(this IMarshalingMetadata metadata)
            => metadata.Get(IDebugInfoMetadata.FallbackSingleton).Info;
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
        FixedLength = 1,
        /// <summary>
        /// To the end of byte segment
        /// </summary>
        WholeSegment = 2,
    }
    public interface IStringLengthMetadata
    {
        int Length { get; }
    }
    public interface ICollectionCountMetadata
    {
        int Count { get; }
    }
    public interface ICollectionReadWhileMetadata<TCollectionItem>
    {
        bool ReadWhile(List<(int, TCollectionItem?)> currentCollection, IDataBuffer data, IMarshalingMetadata meta, IOffsetStack stack);

        public static Fallback FallbackSingleton = new Fallback();
        public class Fallback : ICollectionReadWhileMetadata<TCollectionItem>
        {
            public bool ReadWhile(List<(int, TCollectionItem?)> currentCollection, IDataBuffer data, IMarshalingMetadata meta, IOffsetStack stack)
                => true;
        }
    }
    public interface IDebugInfoMetadata
    {
        string Info { get; }

        public static Fallback FallbackSingleton = new Fallback();
        public class Fallback : IDebugInfoMetadata
        {
            public string Info => string.Empty;
        }
    }

    public class DefaultMarshalingMetadata : IMarshalingMetadata
    {
        [return: NotNullIfNotNull(nameof(fallbackValue))]
        public T? Get<T>(T? fallbackValue)
            => GetAll<T>().DefaultIfEmpty(fallbackValue).First();

        public IEnumerable<T> GetAll<T>()
            => metadata.OfType<T>();

        private List<object> metadata = new List<object>();
        public void Add(object value)
            => metadata.Add(value);
    }
}
