namespace BinaryFile.MarshalingDI.Context
{
    public class ValueFeature<T> : IFeature<T>
    {
        private readonly T value;

        public ValueFeature(T value, string? name = null, int maxEffectiveAge = 0)
        {
            this.value = value;
            Name = name;
            MaxEffectiveAge = maxEffectiveAge;
        }
        public string? Name { get; }

        public int MaxEffectiveAge { get; }

        public T GetValue() => value;
    }
}