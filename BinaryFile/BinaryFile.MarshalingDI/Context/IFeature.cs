namespace BinaryFile.MarshalingDI.Context
{
    public interface IFeature
    {
        string? Name { get; }
        int MaxEffectiveAge { get; }
    }
    public interface IFeature<T> : IFeature
    {
        public T GetValue();
    }
}