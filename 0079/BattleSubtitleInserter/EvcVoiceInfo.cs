namespace BattleSubtitleInserter
{
    public class EvcVoiceInfo(string VoiceName, int Delay, int Duration)
    {
        public string VoiceName { get; } = VoiceName;
        public int Delay { get; } = Delay;
        public int Duration { get; } = Duration;

        public override string ToString()
        {
            return VoiceName;
        }
    }
}
