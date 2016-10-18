namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    public static class EnumUtils
    {
        private enum Dummy { }

        private static MethodInfo enumTryParse;

        private static MethodInfo EnumTryParse
        {
            get
            {
                if (enumTryParse == null) {
                    Expression<Func<string, Dummy, bool>> expr = (s, r) => Enum.TryParse(s, false, out r);
                    enumTryParse = ((MethodCallExpression)expr.Body).Method.GetGenericMethodDefinition();
                }
                return enumTryParse;
            }
        }

        public static bool TryParse(Type type, string str, out object result)
        {
            var method = EnumTryParse.MakeGenericMethod(type);

            var parameters = new object[] { str, false, null };
            if ((bool)method.Invoke(null, parameters)) {
                result = parameters[2];
                return true;
            }

            result = null;
            return false;
        }
    }
}
