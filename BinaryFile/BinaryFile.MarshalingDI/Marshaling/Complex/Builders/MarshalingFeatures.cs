using BinaryFile.MarshalingDI.Context;

namespace BinaryFile.MarshalingDI.Marshaling.Complex
{
    public class MarshalingFeatures
    {
        public List<IHierarchicalFeatureSet.IFeatureWrapper> ReadFeatures =
            new List<IHierarchicalFeatureSet.IFeatureWrapper>();
        public List<IHierarchicalFeatureSet.IFeatureWrapper> WriteFeatures =
            new List<IHierarchicalFeatureSet.IFeatureWrapper>();
    }
}
