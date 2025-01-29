using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ReflectionHelper
{
    public static class TypeHierarchyHelper
    {
        public static IEnumerable<Type> EnumerateTypeHierarchy(this Type type)
        {
            while (type != null)
            {
                yield return type;
                type = type.BaseType!;
            }
        }
    }
}
