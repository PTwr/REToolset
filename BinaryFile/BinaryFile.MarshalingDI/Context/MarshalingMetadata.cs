using Autofac;
using BinaryFile.MarshalingDI.DAL;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static BinaryFile.MarshalingDI.Context.HierarchicalFeatureSet;

namespace BinaryFile.MarshalingDI.Context
{
    public enum EMetadataNames
    {
        CurentObject,
        ParentObject,
        DebugInfo,
        CollectionCount,
        StringLength,
        CollectionReadWhile,
        FileName,
    }
    public static class MarshalingMetadata
    {
        public static IHierarchicalFeatureSet GetFeatures(this ILifetimeScope container)
            => container.Resolve<IHierarchicalFeatureSet>();

        public static string GetFileName(this IHierarchicalFeatureSet hierarchicalFeatureSet)
            => hierarchicalFeatureSet.Get(string.Empty, EMetadataNames.FileName.ToString());
        public static void SetFileName(this IHierarchicalFeatureSet hierarchicalFeatureSet)
            => hierarchicalFeatureSet.AddValueFeature(string.Empty, int.MaxValue, EMetadataNames.FileName.ToString());

        public static EMarshalingEndianness GetEndianness(this IHierarchicalFeatureSet hierarchicalFeatureSet)
            => hierarchicalFeatureSet.Get(EMarshalingEndianness.LittleEndian);
        public static bool IsLittleEndian(this  IHierarchicalFeatureSet hierarchicalFeatureSet)
            => hierarchicalFeatureSet.GetEndianness() == EMarshalingEndianness.LittleEndian;

        public static Encoding GetTextEncoding(this IHierarchicalFeatureSet hierarchicalFeatureSet)
            => hierarchicalFeatureSet.Get(Encoding.ASCII);
        public static EStringLengthStyle GetStringLengthStyle(this IHierarchicalFeatureSet hierarchicalFeatureSet)
            => hierarchicalFeatureSet.Get(EStringLengthStyle.NullTerminator);
        public static bool HasStringLength(this IHierarchicalFeatureSet hierarchicalFeatureSet, out int count)
            => hierarchicalFeatureSet.TryGet<int>(out count, EMetadataNames.StringLength.ToString());

        public static string GetDebugInfo(this IHierarchicalFeatureSet hierarchicalFeatureSet)
            => Environment.NewLine + string.Join(Environment.NewLine, hierarchicalFeatureSet.GetAll<string>(EMetadataNames.DebugInfo.ToString()).Select(x => x.Value));

        public static bool HasCollectionCount(this IHierarchicalFeatureSet hierarchicalFeatureSet, out int count)
            => hierarchicalFeatureSet.TryGet<int>(out count, EMetadataNames.CollectionCount.ToString());
        public static bool CollectionReadWhile(this IHierarchicalFeatureSet hierarchicalFeatureSet)
            => hierarchicalFeatureSet.Get(true, EMetadataNames.CollectionReadWhile.ToString());

        public static bool TryGetParent<T>(this IHierarchicalFeatureSet hierarchicalFeatureSet, [NotNullWhen(true)] out T? parent)
            => hierarchicalFeatureSet.TryGet<T>(out parent, EMetadataNames.ParentObject.ToString());
        public static T GetParent<T>(this IHierarchicalFeatureSet hierarchicalFeatureSet)
            => hierarchicalFeatureSet.GetRequired<T>(EMetadataNames.ParentObject.ToString());
        public static T GetParent<T>(this ILifetimeScope container)
            => container.GetFeatures().GetParent<T>();

        public static bool TryGetCurentObject<T>(this IHierarchicalFeatureSet hierarchicalFeatureSet, [NotNullWhen(true)] out T? parent)
            => hierarchicalFeatureSet.TryGet<T>(out parent, EMetadataNames.CurentObject.ToString());
        public static T GetCurentObject<T>(this IHierarchicalFeatureSet hierarchicalFeatureSet)
            => hierarchicalFeatureSet.GetRequired<T>(EMetadataNames.CurentObject.ToString());
    }

    public enum EMarshalingEndianness
    {
        BigEndian = 0,
        LittleEndian = 1,
    }
    public enum EStringLengthStyle
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
}
