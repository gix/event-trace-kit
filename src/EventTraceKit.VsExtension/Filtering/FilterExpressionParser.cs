namespace EventTraceKit.VsExtension.Filtering
{
    using System;
    using System.Collections.Generic;

    public class FilterExpressionParser
    {
        private readonly FilterExpressionLexer lexer;
        private readonly Stack<Token> operators = new Stack<Token>();
        private readonly Stack<ExpressionSyntax> operands = new Stack<ExpressionSyntax>();

        public FilterExpressionParser(FilterExpressionLexer lexer)
        {
            this.lexer = lexer;
        }

        public ExpressionSyntax ParseExpression()
        {
            var token = new Token();
            var prevTokenKind = TokenKind.Unknown;
            while (lexer.Lex(ref token)) {
                if (token.Is(TokenKind.NumericConstant)) {
                    operands.Push(new LiteralExpressionSyntax(SyntaxKind.NumericLiteralExpression, token.GetText()));
                } else if (token.Is(TokenKind.StringLiteral)) {
                    operands.Push(new LiteralExpressionSyntax(SyntaxKind.StringLiteralExpression, token.GetText()));
                } else if (token.Is(TokenKind.GuidLiteral)) {
                    operands.Push(new LiteralExpressionSyntax(SyntaxKind.GuidLiteralExpression, token.GetText()));
                } else if (token.Is(TokenKind.Identifier)) {
                    operands.Push(new IdentifierNameSyntax(token));
                } else if (IsOperator(token.Kind)) {
                    if (IsUnaryOperator(token.Kind, prevTokenKind)) {
                        token.Flags |= TokenFlags.Unary;
                    } else {
                        var prec = GetOpPrecedence(token.Kind);
                        var isLeftAssoc = prec != PrecedenceLevel.Conditional;

                        while (operators.Count > 0) {
                            var otherKind = operators.Peek().Kind;
                            var otherPrec = GetOpPrecedence(otherKind);
                            bool b = ((otherPrec > prec) || (otherPrec >= prec && isLeftAssoc)) && (otherKind != TokenKind.LParen);
                            if (!b)
                                break;

                            PopOperator();
                        }
                    }

                    operators.Push(token);
                } else if (token.Is(TokenKind.LParen)) {
                    operators.Push(token);
                } else if (token.Is(TokenKind.RParen)) {
                    while (operators.Count > 0 && operators.Peek().Kind != TokenKind.LParen)
                        PopOperator();

                    if (operators.Count == 0)
                        throw new Exception("Mismatched parentheses");

                    PopOperator();
                } else {
                    throw new Exception($"Unknown token '{token.GetText()}'");
                }

                prevTokenKind = token.Kind;
            }

            while (operators.Count > 0) {
                token = operators.Peek();
                if (token.Is(TokenKind.LParen) || token.Is(TokenKind.RParen))
                    throw new Exception("Mismatched parentheses");

                PopOperator();
            }

            if (operands.Count != 1)
                throw new Exception("Invalid expression");

            return operands.Pop();
        }

        private bool IsUnaryOperator(TokenKind kind, TokenKind prevKind)
        {
            bool isUnaryToken =
                kind == TokenKind.Plus ||
                kind == TokenKind.Minus ||
                kind == TokenKind.Exclaim;

            return isUnaryToken &&
                   (prevKind == TokenKind.Unknown || prevKind == TokenKind.LParen || IsOperator(prevKind));
        }

        private void PopOperator()
        {
            if (operators.Count == 0)
                throw new Exception("Operator stack empty");

            var token = operators.Pop();
            if ((token.Flags & TokenFlags.Unary) == 0 && IsBinaryOperator(token.Kind)) {
                var rhs = operands.Pop();
                var lhs = operands.Pop();
                operands.Push(new BinaryExpressionSyntax(GetBinExprKind(token.Kind), lhs, rhs));
            } else if (token.Kind == TokenKind.LParen) {
            } else {
                var operand = operands.Pop();
                operands.Push(new PrefixUnaryExpressionSyntax(GetUnaryExprKind(token.Kind), operand));
            }
        }

        private SyntaxKind GetBinExprKind(TokenKind kind)
        {
            return kind switch
            {
                TokenKind.Less => SyntaxKind.LessThanExpression,
                TokenKind.LessLess => SyntaxKind.LeftShiftExpression,
                TokenKind.LessEqual => SyntaxKind.LessThanOrEqualExpression,
                TokenKind.Greater => SyntaxKind.GreaterThanExpression,
                TokenKind.GreaterGreater => SyntaxKind.RightShiftExpression,
                TokenKind.GreaterEqual => SyntaxKind.GreaterThanOrEqualExpression,
                TokenKind.ExclaimEqual => SyntaxKind.NotEqualExpression,
                TokenKind.Amp => SyntaxKind.BitwiseAndExpression,
                TokenKind.AmpAmp => SyntaxKind.LogicalAndExpression,
                TokenKind.Pipe => SyntaxKind.BitwiseOrExpression,
                TokenKind.PipePipe => SyntaxKind.LogicalOrExpression,
                TokenKind.Plus => SyntaxKind.AddExpression,
                TokenKind.Minus => SyntaxKind.SubtractExpression,
                TokenKind.Percent => SyntaxKind.ModuloExpression,
                TokenKind.Star => SyntaxKind.MultiplyExpression,
                TokenKind.Slash => SyntaxKind.DivideExpression,
                TokenKind.EqualEqual => SyntaxKind.EqualExpression,
                TokenKind.Caret => SyntaxKind.ExclusiveOrExpression,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }

        private static SyntaxKind GetUnaryExprKind(TokenKind kind)
        {
            return kind switch
            {
                TokenKind.Plus => SyntaxKind.UnaryPlusExpression,
                TokenKind.Minus => SyntaxKind.UnaryMinusExpression,
                TokenKind.Exclaim => SyntaxKind.LogicalNotExpression,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }

        private static bool IsOperator(TokenKind kind)
        {
            return
                IsBinaryOperator(kind) ||
                kind == TokenKind.Exclaim
                ;
        }

        private static bool IsBinaryOperator(TokenKind kind)
        {
            return
                // Comparisons
                kind == TokenKind.EqualEqual ||
                kind == TokenKind.ExclaimEqual ||
                kind == TokenKind.Less ||
                kind == TokenKind.LessEqual ||
                kind == TokenKind.Greater ||
                kind == TokenKind.GreaterEqual ||
                // Mathematical operators
                kind == TokenKind.Plus ||
                kind == TokenKind.Minus ||
                kind == TokenKind.Star ||
                kind == TokenKind.Slash ||
                kind == TokenKind.Amp ||
                kind == TokenKind.Pipe ||
                kind == TokenKind.Caret ||
                kind == TokenKind.LessLess ||
                kind == TokenKind.GreaterGreater ||
                kind == TokenKind.Percent ||
                // Logical operators
                kind == TokenKind.AmpAmp ||
                kind == TokenKind.PipePipe
                ;
        }

        private enum PrecedenceLevel
        {
            Unknown,        // Not an operator.
            Conditional,    // ?
            LogicalOr,      // ||
            LogicalAnd,     // &&
            InclusiveOr,    // |
            ExclusiveOr,    // ^
            And,            // &
            Equality,       // ==, !=
            Relational,     // >=, <=, >, <
            Shift,          // <<, >>
            Additive,       // -, +
            Multiplicative, // *, /, %
            Unary,          // !v, +v, -v
        }

        private static PrecedenceLevel GetOpPrecedence(TokenKind kind)
        {
            switch (kind) {
                default: return PrecedenceLevel.Unknown;
                case TokenKind.Question: return PrecedenceLevel.Conditional;
                case TokenKind.PipePipe: return PrecedenceLevel.LogicalOr;
                case TokenKind.AmpAmp: return PrecedenceLevel.LogicalAnd;
                case TokenKind.Pipe: return PrecedenceLevel.InclusiveOr;
                case TokenKind.Caret: return PrecedenceLevel.ExclusiveOr;
                case TokenKind.Amp: return PrecedenceLevel.And;
                case TokenKind.ExclaimEqual:
                case TokenKind.EqualEqual: return PrecedenceLevel.Equality;
                case TokenKind.Less:
                case TokenKind.LessEqual:
                case TokenKind.Greater:
                case TokenKind.GreaterEqual: return PrecedenceLevel.Relational;
                case TokenKind.LessLess:
                case TokenKind.GreaterGreater: return PrecedenceLevel.Shift;
                case TokenKind.Plus:
                case TokenKind.Minus: return PrecedenceLevel.Additive;
                case TokenKind.Percent:
                case TokenKind.Slash:
                case TokenKind.Star: return PrecedenceLevel.Multiplicative;
                case TokenKind.Exclaim: return PrecedenceLevel.Unary;
            }
        }
    }
}
