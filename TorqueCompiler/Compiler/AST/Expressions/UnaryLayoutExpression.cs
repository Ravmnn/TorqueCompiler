using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public interface IUnaryLayoutExpressionFactory
{
    public static abstract UnaryLayoutExpression Create(Expression right, TokenType @operator, Span location);
}


public abstract class UnaryLayoutExpression(Expression right, TokenType @operator, Span location) : Expression(location)
{
    public Expression Right { get; } = right;
    public TokenType Operator { get; } = @operator;
}
