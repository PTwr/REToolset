﻿using R79JAFshared;

namespace BattleSubtitleInserter
{
    public static class Env
    {
        public static string CleanCopyFilesDirectory => @"C:\G\Wii\R79JAF_clean\DATA\files";
        public static string DirtyCopyFilesDirectory => @"C:\G\Wii\R79JAF_clean\DATA\files";
        public static string PatchAssetDirectory => @"C:\G\Wii\R79JAF patch assets";

        public static string VoiceFileAbsolutePath(string voiceFile)
            => $@"{CleanCopyFilesDirectory}\sound\stream\{voiceFile}.brstm";

        public static string EVCFileAbsolutePath(string evc)
            => $@"{CleanCopyFilesDirectory}\evc\{evc}.arc";

        public static string BootArcAbsolutePath()
            => $@"{CleanCopyFilesDirectory}\boot\boot.arc";

        public static string CleanToDirty(string path)
        {
            path = GetRelativePath(CleanCopyFilesDirectory, path);
            return $@"{DirtyCopyFilesDirectory}\{path}";
        }

        static SubtitleImgCutInGenerator _gen;
        public static SubtitleImgCutInGenerator PrepSubGen()
        {
            if (_gen is not null) return _gen;
            var gen = new SubtitleImgCutInGenerator(
                $@"{PatchAssetDirectory}\SubtitleAssets",
                $@"{CleanCopyFilesDirectory}\sound\stream",
                $@"{PatchAssetDirectory}\subtitleTranslation",
                $@"{PatchAssetDirectory}\tempDir",
                $@"{DirtyCopyFilesDirectory}\_2d\ImageCutIn"
                );

            return _gen = gen;
        }

        /// <summary>
        /// Wraps Path.GetRelativePath to provide some useful functions
        /// </summary>
        /// <param name="relativeTo"></param>
        /// <param name="path"></param>
        /// <param name="doNothingForSamePath">returns path without change if its same as relativeTo</param>
        /// <returns></returns>
        public static string GetRelativePath(string relativeTo, string path, bool doNothingForSamePath = true)
        {
            if (doNothingForSamePath && string.Equals(relativeTo, path, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
            return Path.GetRelativePath(relativeTo, path);
        }
    }
}