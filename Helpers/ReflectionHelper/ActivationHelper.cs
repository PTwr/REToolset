using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        public static Func<T> PrepareActivationLambda<T>()
        {
            //public, non-static, parameterless ctor
            var ctor = typeof(T).GetConstructor(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, []);

            if (ctor is null) throw new InvalidOperationException($"Type {typeof(T).FullName} does not contain parameterless constructor!");

            var expr = Expression.New(ctor);
            var lamb = Expression.Lambda<Func<T>>(expr, []);
            var func = lamb.Compile();

            return func;
        }
    }
}
