using Autofac;
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
        public static IFeatureSetStack GetFeatures(this ILifetimeScope container)
            => container.Resolve<IFeatureSetStack>();
        public static TFeature GetValue<TFeature>(this IFeatureSet features, TFeature fallback, EMetadataNames metadataName)
            => features.GetValue(fallback, metadataName.ToString());
        public static void AddFeature<TFeature>(this IFeatureSet features, TFeature value, EMetadataNames metadataName, int maxAge)
            => features.AddFeature(new ValueFeature<TFeature>(value, metadataName.ToString(), maxAge));

        public static string GetFileName(this IFeatureSet features)
            => features.GetValue(string.Empty, EMetadataNames.FileName);
        public static void SetFileName(this IFeatureSet features, string fileName)
            => features.AddFeature(fileName, EMetadataNames.FileName, int.MaxValue);

        public static EMarshalingEndianness GetEndianness(this IFeatureSet features)
            => features.GetValue(EMarshalingEndianness.LittleEndian);
        public static bool IsLittleEndian(this  IFeatureSet features)
            => features.GetEndianness() == EMarshalingEndianness.LittleEndian;

        public static Encoding GetTextEncoding(this IFeatureSet features)
            => features.GetValue(Encoding.ASCII);
        public static EStringLengthStyle GetStringLengthStyle(this IFeatureSet features)
            => features.GetValue(EStringLengthStyle.NullTerminator);
        public static bool HasStringLength(this IFeatureSet features, out int count)
            => features.TryGetValue<int>(out count, EMetadataNames.StringLength.ToString());

        public static string GetDebugInfo(this IFeatureSet features)
            => string.Join(Environment.NewLine, features.GetAll<string>(EMetadataNames.DebugInfo.ToString()).Select(x => x.GetValue()));

        public static bool HasCollectionCount(this IFeatureSet features, out int count)
            => features.TryGetValue<int>(out count, EMetadataNames.CollectionCount.ToString());
        public static bool CollectionReadWhile(this IFeatureSet features)
            => features.GetValue(true, EMetadataNames.CollectionReadWhile);

        public static bool TryGetParent<T>(this IFeatureSet features, [NotNullWhen(true)] out T? parent)
            => features.TryGetValue<T>(out parent, EMetadataNames.ParentObject.ToString());
        public static T GetParent<T>(this IFeatureSet features)
            => features.GetRequiredValue<T>(EMetadataNames.ParentObject.ToString());
        public static T GetParent<T>(this ILifetimeScope container)
            => container.GetFeatures().GetParent<T>();

        public static bool TryGetCurentObject<T>(this IFeatureSet features, [NotNullWhen(true)] out T? parent)
            => features.TryGetValue<T>(out parent, EMetadataNames.CurentObject.ToString());
        public static T GetCurentObject<T>(this IFeatureSet features)
            => features.GetRequiredValue<T>(EMetadataNames.CurentObject.ToString());
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
