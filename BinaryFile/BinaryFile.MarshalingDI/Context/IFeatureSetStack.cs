using Autofac;

namespace BinaryFile.MarshalingDI.Context
{
    public interface IFeatureSetStack : IFeatureSet
    {
        void Pop();
        void Push(string name);

        IFeatureSet this[string name] { get; }
        IFeatureSet this[int age] { get; }
    }
}