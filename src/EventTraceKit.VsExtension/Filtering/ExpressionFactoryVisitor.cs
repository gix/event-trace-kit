namespace EventTraceKit.VsExtension.Filtering
{
    using System;
    using System.Linq.Expressions;

    public class ExpressionFactoryVisitor : FilterSyntaxVisitor<Expression>
    {
        public static Expression Convert(ExpressionSyntax expr)
        {
            return new ExpressionFactoryVisitor().Visit(expr);
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
            if (expr.Kind == SyntaxKind.StringLiteralExpression)
                return Expression.Constant(expr.Text.Substring(1, expr.Text.Length - 2));
            if (expr.Kind == SyntaxKind.GuidLiteralExpression)
                return CreateValueExpression(Guid.Parse(expr.Text));

            return Expression.Constant(expr.Text);
        }

        public override Expression VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax expr)
        {
            switch (expr.Kind) {
                case SyntaxKind.UnaryPlusExpression:
                    return Expression.UnaryPlus(Visit(expr.Operand));
                case SyntaxKind.UnaryMinusExpression:
                    return Expression.Negate(Visit(expr.Operand));
                case SyntaxKind.LogicalNotExpression:
                    return Expression.Not(Visit(expr.Operand));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override Expression VisitBinaryExpression(BinaryExpressionSyntax expr)
        {
            var exprType = GetExpressionType(expr.Kind);
            var left = Visit(expr.Left);
            var right = Visit(expr.Right);

            if (left.Type != right.Type) {
                if (!TryConvert(ref right, left.Type) &&
                    !TryConvert(ref left, right.Type))
                    throw new ArgumentOutOfRangeException($"Incompatible operand types '{left}' and '{right}' for {exprType}");
            }

            return Expression.MakeBinary(exprType, left, right);
        }

        private bool TryConvert(ref Expression expr, Type targetType)
        {
            if (expr.NodeType != ExpressionType.Constant)
                return false;

            var value = ((ConstantExpression)expr).Value;
            if (expr.Type == typeof(string)) {
                var str = (string)value;

                if (targetType == typeof(sbyte)) {
                    expr = Expression.Constant(sbyte.Parse(str));
                    return true;
                }

                if (targetType == typeof(byte)) {
                    expr = Expression.Constant(byte.Parse(str));
                    return true;
                }

                if (targetType == typeof(short)) {
                    expr = Expression.Constant(short.Parse(str));
                    return true;
                }

                if (targetType == typeof(ushort)) {
                    expr = Expression.Constant(ushort.Parse(str));
                    return true;
                }

                if (targetType == typeof(int)) {
                    expr = Expression.Constant(int.Parse(str));
                    return true;
                }

                if (targetType == typeof(uint)) {
                    expr = Expression.Constant(uint.Parse(str));
                    return true;
                }

                if (targetType == typeof(long)) {
                    expr = Expression.Constant(long.Parse(str));
                    return true;
                }

                if (targetType == typeof(ulong)) {
                    expr = Expression.Constant(ulong.Parse(str));
                    return true;
                }

                if (targetType == typeof(Guid)) {
                    expr = CreateValueExpression(Guid.Parse(str));
                    return true;
                }
            }

            return false;
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
    }
}
