using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryDataHelper
{
    //TODO Dictionary<TItem, autoincrement> might be much faster? Dict would allow deletion without reusing index, implementation backed by list can't offer deletion
    /// <summary>
    /// Slow reimagination of HashSet for when you need to know index of items
    /// </summary>
    /// <remarks>NOT thread safe</remarks>
    /// <typeparam name="T">Type of item to store</typeparam>
    public class DistinctList<T>
    {
        public DistinctList()
        {

        }
        public DistinctList(IEnumerable<T> items)
        {
            data = items.Distinct().ToList();
        }

        List<T> data = new List<T>();

        /// <summary>
        /// Readonly copy of current dataset
        /// Ahtung! Reference types are not cloned, exercise all due caution! :)
        /// </summary>
        public IEnumerable<T> Data => data.AsReadOnly();

        public void Clear() => data.Clear();
        /// <summary>
        /// Inserts new item if its not already on list
        /// </summary>
        /// <param name="item">Item to be inserted</param>
        /// <returns>Index of inserted item, or its preexisting duplicate</returns>
        public int Add(T item)
        {
            if (data.Contains(item)) return data.IndexOf(item);

            data.Add(item);

            //index of freshly added item
            return data.Count - 1;
        }

        public int Count => data.Count;

        public T this[int index]
        {
            get
            {
                if (index < 0 || index > data.Count) throw new Exception($"List index '{index}' out of bounds!");

                return data[index];
            }
        }
    }
}
