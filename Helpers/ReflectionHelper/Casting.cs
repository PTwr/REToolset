using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReflectionHelper
{
    public static class Casting
    {
        public static bool IsAssignableTo<TInterface>(this Type type)
        {
            return type.IsAssignableTo(typeof(TInterface));
        }
        public static bool IsAssignableTo<TInterface>(this PropertyInfo prop)
        {
            return prop.PropertyType.IsAssignableTo(typeof(TInterface));
        }
        public static bool IsArrayOf<T>(this Type type)
        {
            return type == typeof(T[]);
        }
    }
}
