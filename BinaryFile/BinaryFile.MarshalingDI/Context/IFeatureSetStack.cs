using Autofac;

namespace BinaryFile.MarshalingDI.Context
{
    public interface IFeatureSetStack : IFeatureSet
    {
        void Pop();
        void Push();

        IFeatureSet this[int age] { get; }
    }
}