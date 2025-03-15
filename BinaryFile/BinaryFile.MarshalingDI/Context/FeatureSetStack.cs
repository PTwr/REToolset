namespace BinaryFile.MarshalingDI.Context
{
    public class FeatureSetStack : FeatureSetWrapper, IFeatureSetStack, IFeatureSet
    {
        List<IFeatureSet> featureSetStack = [new FeatureSet(0, null)];

        public IFeatureSet this[int age] =>
            featureSetStack.ElementAtOrDefault(age)
            ??
            throw new IndexOutOfRangeException($"Feature Set of age '{age}' not found on stack.");

        protected override IFeatureSet Current 
            => featureSetStack.First();

        public void Pop() 
            => featureSetStack.RemoveAt(0);

        public void Push() 
            => featureSetStack.Insert(0, new FeatureSet(featureSetStack.Count, Current));
    }
}