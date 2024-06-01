namespace BattleSubtitleInserter
{
    public static class Env
    {
        public static string CleanCopyFilesDirectory => @"C:\G\Wii\R79JAF_clean\DATA\files";

        public static string VoiceFileAbsolutePath(string voiceFile)
            => $@"{CleanCopyFilesDirectory}\sound\stream\{voiceFile}.brstm";

        public static string EVCFileAbsolutePath(string evc)
            => $@"{CleanCopyFilesDirectory}\evc\{evc}.arc";

        public static string BootArcAbsolutePath()
            => $@"{CleanCopyFilesDirectory}\boot\boot.arc";
    }
}
