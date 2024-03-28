using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReflectionHelper
{
    public static class Expressions
    {
        private static MemberInfo? GetGetterMemberInfo<TDeclaringType, TFieldType>(Expression<Func<TDeclaringType, TFieldType>> getter)
            => (getter.Body as MemberExpression)?.Member;

        private static bool IsMemberReadonly(MemberInfo memberInfo)
        {
            var setMethod = (memberInfo as PropertyInfo)?.SetMethod;
            bool isReadOnly =
                ((memberInfo is PropertyInfo) && (setMethod is null || setMethod.Attributes.HasFlag(MethodAttributes.Private)))
                ||
                ((memberInfo as FieldInfo)?.IsInitOnly == true);
            return isReadOnly;
        }

        private static Expression<Action<TDeclaringType, TFieldType>> GenerateSetter<TDeclaringType, TFieldType>(MemberInfo memberInfo)
        {
            var objVar = Expression.Variable(typeof(TDeclaringType));
            var valVar = Expression.Variable(typeof(TFieldType));

            var memberAccess = Expression.MakeMemberAccess(objVar, memberInfo);
            var assignment = Expression.Assign(memberAccess, valVar);
            var expr = Expression.Lambda<Action<TDeclaringType, TFieldType>>(assignment, objVar, valVar);

            return expr;
        }

        public static bool TryGenerateToSetter<TDeclaringType, TFieldType>(
            this Expression<Func<TDeclaringType, TFieldType>> getter,
            [NotNullWhen(returnValue: true)] out Expression<Action<TDeclaringType, TFieldType>>? setter)
        {
            setter = null!;
            var memberInfo = GetGetterMemberInfo(getter);

            if (memberInfo == null)
                return false;
            if (IsMemberReadonly(memberInfo))
                return false;

            setter = GenerateSetter<TDeclaringType, TFieldType>(memberInfo);
            return true;
        }

        public static Expression<Action<TDeclaringType, TFieldType>> GenerateToSetter<TDeclaringType, TFieldType>(this Expression<Func<TDeclaringType, TFieldType>> getter)
        {
            var memberInfo = GetGetterMemberInfo(getter);

            if (memberInfo == null) 
                throw new ArgumentException("Not a getter expression, Expression must be simple single-level field or property access reference.");
            if (IsMemberReadonly(memberInfo)) 
                throw new ArgumentException("Indicated field is readonly or has private setter.");

            return GenerateSetter<TDeclaringType, TFieldType>(memberInfo);
        }
    }
}
