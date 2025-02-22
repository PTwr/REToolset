namespace BinaryFile.MarshalingDI.Context
{
    public class FeatureSetStack : FeatureSetWrapper, IFeatureSetStack, IFeatureSet
    {
        List<IFeatureSet> featureSetStack = [new FeatureSet("root", 0, null)];

        public IFeatureSet this[string name] =>
            featureSetStack.FirstOrDefault(x => x.Name == name)
            ??
            throw new IndexOutOfRangeException($"Feature Set of name '{name}' not found on stack.");

        public IFeatureSet this[int age] =>
            featureSetStack.ElementAtOrDefault(age)
            ??
            throw new IndexOutOfRangeException($"Feature Set of age '{age}' not found on stack.");

        protected override IFeatureSet Current 
            => featureSetStack.First();

        public void Pop() 
            => featureSetStack.RemoveAt(0);

        public void Push(string name) 
            => featureSetStack.Insert(0, new FeatureSet(name, featureSetStack.Count, Current));
    }
}