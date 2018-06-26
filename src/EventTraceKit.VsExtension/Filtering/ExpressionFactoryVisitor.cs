namespace EventTraceKit.VsExtension.Filtering
{
    using System;
    using System.Globalization;
    using System.Linq.Expressions;
    using EventTraceKit.VsExtension.Extensions;

    public class ExpressionFactoryVisitor : FilterSyntaxVisitor<Expression>
    {
        public static Expression Convert(ExpressionSyntax expr)
        {
            var converted = new ExpressionFactoryVisitor().Visit(expr);
            if (converted.Type != typeof(bool))
                throw new InvalidOperationException("Input expression is not boolean.");
            return converted;
        }

        public override Expression VisitIdentifierName(IdentifierNameSyntax expr)
        {
            var builder = TraceLogFilterBuilder.Instance;
            var name = expr.Identifier.GetText();

            switch (name.ToLowerInvariant()) {
                case "providerid": return builder.ProviderId;

                case "processid": return builder.ProcessId;
                case "pid": return builder.ProcessId;
                case "threadid": return builder.ThreadId;
                case "tid": return builder.ThreadId;
                case "activityid": return builder.ActivityId;
                case "relatedactivityid": return builder.RelatedActivityId;

                case "id": return builder.Id;
                case "version": return builder.Version;
                case "channel": return builder.Channel;
                case "level": return builder.Level;
                case "opcode": return builder.Opcode;
                case "task": return builder.Task;
                case "keyword": return builder.Keyword;

                default:
                    throw new InvalidOperationException($"Unknown identifier '{name}'");
            }
        }

        public override Expression VisitLiteralExpression(LiteralExpressionSyntax expr)
        {
            switch (expr.Kind) {
                case SyntaxKind.StringLiteralExpression:
                    return Expression.Constant(expr.Text.Substring(1, expr.Text.Length - 2));
                case SyntaxKind.GuidLiteralExpression:
                    return CreateValueExpression(Guid.Parse(expr.Text));
                case SyntaxKind.NumericLiteralExpression:
                    return Expression.Constant(ConvertToNumber(expr.Text));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static object ConvertToNumber(string numericLiteral)
        {
            if (numericLiteral.StartsWith("0x")
                && ulong.TryParse(numericLiteral.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var intVal)
                || ulong.TryParse(numericLiteral, NumberStyles.None, CultureInfo.InvariantCulture, out intVal)) {
                if (intVal <= byte.MaxValue)
                    return (byte)intVal;
                if (intVal <= ushort.MaxValue)
                    return (ushort)intVal;
                if (intVal <= uint.MaxValue)
                    return (uint)intVal;
                return intVal;
            }

            if (numericLiteral.IndexOfAny(new[] { '.', 'E', 'e' }) != -1 &&
                double.TryParse(numericLiteral, NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var floatVal)) {
                return floatVal;
            }

            throw new ArgumentException($"Invalid numeric literal '{numericLiteral}'");
        }

        public override Expression VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax expr)
        {
            switch (expr.Kind) {
                case SyntaxKind.UnaryPlusExpression:
                    return Expression.UnaryPlus(Visit(expr.Operand));
                case SyntaxKind.UnaryMinusExpression:
                    return Expression.Negate(VerifyNegate(Visit(expr.Operand)));
                case SyntaxKind.LogicalNotExpression:
                    return Expression.Not(Visit(expr.Operand));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Expression VerifyNegate(Expression expr)
        {
            if (expr is ConstantExpression ce && ce.Type.IsArithmetic() && ce.Type.IsUnsignedInt())
                throw new ArgumentException("Unsigned integer cannot be negative.");
            return expr;
        }

        public override Expression VisitBinaryExpression(BinaryExpressionSyntax expr)
        {
            var exprType = GetExpressionType(expr.Kind);
            var left = Visit(expr.Left);
            var right = Visit(expr.Right);

            if (exprType == ExpressionType.LeftShift || exprType == ExpressionType.RightShift) {
                if (right.Type != typeof(int) && CanPromote(right.Type, typeof(int)))
                    right = Expression.Convert(right, typeof(int));
            }

            if (left.Type != right.Type) {
                if (!TryConvert(ref right, left.Type) &&
                    !TryConvert(ref left, right.Type)) {
                    string leftType = GetTypeDescription(left.Type);
                    string rightType = GetTypeDescription(right.Type);
                    string operation = GetOperationDescription(exprType);
                    throw new ArgumentException(
                        $"Cannot {operation} incompatible operand types '{leftType}' (for '{left}') and '{rightType}' (for '{right}')");
                }
            }

            return Expression.MakeBinary(exprType, left, right);
        }

        private static bool TryConvert(ref Expression expr, Type targetType)
        {
            if (expr.NodeType == ExpressionType.Constant) {
                var value = ((ConstantExpression)expr).Value;
                if (expr.Type == typeof(string)) {
                    var str = (string)value;

                    if (targetType == typeof(Guid)) {
                        if (!Guid.TryParse(str, out var v))
                            throw new ArgumentException($"Invalid GUID '{str}'.");
                        expr = CreateValueExpression(v);
                        return true;
                    }
                } else if (targetType.IsArithmetic()) {
                    object targetValue;
                    try {
                        targetValue = System.Convert.ChangeType(
                            value, targetType, CultureInfo.InvariantCulture);
                    } catch (Exception) {
                        return false;
                    }

                    expr = Expression.Constant(targetValue);
                    return true;
                }
            } else if (CanPromote(expr.Type, targetType)) {
                expr = Expression.Convert(expr, targetType);
                return true;
            }

            return false;
        }

        private static bool CanPromote(Type sourceType, Type targetType)
        {
            return
                GetIntSize(sourceType, out var sourceBits) &&
                GetIntSize(targetType, out var targetBits) &&
                targetBits >= sourceBits;
        }

        private static bool GetIntSize(Type type, out int bits)
        {
            switch (Type.GetTypeCode(type)) {
                case TypeCode.SByte:
                case TypeCode.Byte:
                    bits = 8;
                    return true;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    bits = 16;
                    return true;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    bits = 32;
                    return true;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    bits = 64;
                    return true;
                default:
                    bits = 0;
                    return false;
            }
        }

        private static Expression CreateValueExpression(Guid value)
        {
            var bytes = value.ToByteArray();
            var a = BitConverter.ToUInt32(bytes, 0);
            var b = BitConverter.ToUInt16(bytes, 4);
            var c = BitConverter.ToUInt16(bytes, 6);
            return Expression.New(
                typeof(Guid).GetConstructor(new[] {
                    typeof(uint), typeof(ushort), typeof(ushort),
                    typeof(byte), typeof(byte), typeof(byte), typeof(byte),
                    typeof(byte), typeof(byte), typeof(byte), typeof(byte)
                }),
                Expression.Constant(a),
                Expression.Constant(b),
                Expression.Constant(c),
                Expression.Constant(bytes[8]),
                Expression.Constant(bytes[9]),
                Expression.Constant(bytes[10]),
                Expression.Constant(bytes[11]),
                Expression.Constant(bytes[12]),
                Expression.Constant(bytes[13]),
                Expression.Constant(bytes[14]),
                Expression.Constant(bytes[15]));
        }

        private static ExpressionType GetExpressionType(SyntaxKind kind)
        {
            switch (kind) {
                case SyntaxKind.AddExpression: return ExpressionType.Add;
                case SyntaxKind.SubtractExpression: return ExpressionType.Subtract;
                case SyntaxKind.MultiplyExpression: return ExpressionType.Multiply;
                case SyntaxKind.DivideExpression: return ExpressionType.Divide;
                case SyntaxKind.ModuloExpression: return ExpressionType.Modulo;
                case SyntaxKind.EqualExpression: return ExpressionType.Equal;
                case SyntaxKind.NotEqualExpression: return ExpressionType.NotEqual;
                case SyntaxKind.BitwiseAndExpression: return ExpressionType.And;
                case SyntaxKind.BitwiseOrExpression: return ExpressionType.Or;
                case SyntaxKind.ExclusiveOrExpression: return ExpressionType.ExclusiveOr;
                case SyntaxKind.LogicalAndExpression: return ExpressionType.AndAlso;
                case SyntaxKind.LogicalOrExpression: return ExpressionType.OrElse;
                case SyntaxKind.LeftShiftExpression: return ExpressionType.LeftShift;
                case SyntaxKind.RightShiftExpression: return ExpressionType.RightShift;
                case SyntaxKind.LessThanExpression: return ExpressionType.LessThan;
                case SyntaxKind.LessThanOrEqualExpression: return ExpressionType.LessThanOrEqual;
                case SyntaxKind.GreaterThanExpression: return ExpressionType.GreaterThan;
                case SyntaxKind.GreaterThanOrEqualExpression: return ExpressionType.GreaterThanOrEqual;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private string GetOperationDescription(ExpressionType exprType)
        {
            switch (exprType) {
                case ExpressionType.Add: return "add";
                case ExpressionType.Subtract: return "subtract";
                case ExpressionType.Multiply: return "multiply";
                case ExpressionType.Divide: return "divide";
                case ExpressionType.Modulo: return "modulo";
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual: return "compare";
                case ExpressionType.And:
                case ExpressionType.Or:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse: return "logically combine";
                case ExpressionType.LeftShift:
                case ExpressionType.RightShift: return "shift";
                default:
                    throw new ArgumentOutOfRangeException(nameof(exprType), exprType, null);
            }
        }

        private string GetTypeDescription(Type type)
        {
            switch (Type.GetTypeCode(type)) {
                case TypeCode.SByte: return "signed 8-bit integer";
                case TypeCode.Byte: return "unsigned 8-bit integer";
                case TypeCode.Int16: return "signed 16-bit integer";
                case TypeCode.UInt16: return "unsigned 16-bit integer";
                case TypeCode.Int32: return "signed 32-bit integer";
                case TypeCode.UInt32: return "unsigned 32-bit integer";
                case TypeCode.Int64: return "signed 64-bit integer";
                case TypeCode.UInt64: return "unsigned 64-bit integer";
                case TypeCode.Single: return "float";
                case TypeCode.Double: return "double";
                case TypeCode.String: return "string";
                case TypeCode.Boolean: return "bool";
            }

            if (type == typeof(Guid))
                return "GUID";

            return type.ToString();
        }
    }
}
