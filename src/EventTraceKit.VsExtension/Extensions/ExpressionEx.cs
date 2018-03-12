namespace EventTraceKit.VsExtension.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class ExpressionEx
    {
        public static T CompileToTransientAssembly<T>(this Expression<T> lambda)
        {
            var id = Guid.NewGuid();
            var assemblyName = new AssemblyName($"DelegateHostAssembly_{id:N}");
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                assemblyName, AssemblyBuilderAccess.RunAndCollect);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule("transient");
            var typeBuilder = moduleBuilder.DefineType("DelegateHost");
            var methodBuilder = typeBuilder.DefineMethod(
                "Execute", MethodAttributes.Public | MethodAttributes.Static);

            lambda.CompileToMethod(methodBuilder);

            var lambdaHost = typeBuilder.CreateType();
            var execute = lambdaHost.GetMethod(methodBuilder.Name);

            return (T)(object)Delegate.CreateDelegate(typeof(T), execute);
        }

        public static Expression IfNull(this Expression expr, Expression ifTrue, Expression ifFalse)
        {
            return Expression.Condition(IsNull(expr), ifTrue, ifFalse);
        }

        public static Expression IsNull(this Expression expr)
        {
            return Expression.Equal(expr, Expression.Constant(null, expr.Type));
        }

        public static Expression AndOr(IEnumerable<Expression> expressions)
        {
            var enumerator = expressions.GetEnumerator();
            try {
                if (!enumerator.MoveNext())
                    return Expression.Constant(true);

                Expression expr = enumerator.Current;
                while (enumerator.MoveNext())
                    expr = Expression.AndAlso(expr, enumerator.Current);
                return expr;
            } finally {
                enumerator.Dispose();
            }
        }

        public static Expression OrElse(IEnumerable<Expression> expressions, bool defaultValue)
        {
            var enumerator = expressions.GetEnumerator();
            try {
                if (!enumerator.MoveNext())
                    return Expression.Constant(defaultValue);

                Expression expr = enumerator.Current;
                while (enumerator.MoveNext())
                    expr = Expression.OrElse(expr, enumerator.Current);
                return expr;
            } finally {
                enumerator.Dispose();
            }
        }
    }
}
