using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReflectionHelper
{
    public class ActivationHelper
    {
        /// <summary>
        /// WIll call ctor(parent) if available, or default parameterless ctor of not
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static TType Activate<TType>(object? parent)
        {
            if (parent is not null)
            {
                var ctor = typeof(TType).GetConstructor([parent.GetType()]);
                if (ctor is not null) return (TType)ctor.Invoke([parent]);
            }
            return Activator.CreateInstance<TType>();
        }
    }
}
