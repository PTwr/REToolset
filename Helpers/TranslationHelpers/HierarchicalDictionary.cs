namespace TranslationHelpers
{
    public class HierarchicalDictionary<TKey, TValue>// : IDictionary<TKey, TValue>
    {
        HierarchicalDictionary<TKey, TValue>? Parent;
        public HierarchicalDictionary()
        {
            
        }
        public HierarchicalDictionary(HierarchicalDictionary<TKey, TValue> parent)
        {
            Parent = parent;
        }

        Dictionary<TKey, TValue> Data = new Dictionary<TKey, TValue>();
        public void AddRange(Dictionary<TKey, TValue> data)
        {
            foreach (var kvp in data)
                Data[kvp.Key] = kvp.Value;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (Data.TryGetValue(key, out value)) return true;
            else if (Parent is not null && Parent.TryGetValue(key, out value)) return true;
            else return false;
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> Values => Parent == null ? Data : Data.Concat(Parent.Values);
    }
}
