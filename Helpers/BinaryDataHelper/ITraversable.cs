using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryDataHelper
{
    public interface ITraversable
    {
        public virtual void TraverseOfType<T>(Action<T> callback, bool recursive = true)
        {
            var items = recursive ? DescendantsOfType<T>() : ChildrenOfType<T>();

            foreach (var item in items)
            {
                callback(item);
            }
        }

        IEnumerable<T> ChildrenOfType<T>();
        IEnumerable<T> DescendantsOfType<T>();
    }
}
