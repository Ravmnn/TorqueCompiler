using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public interface IBinaryLayoutExpressionFactory
{
    public static abstract BinaryLayoutExpression Create(Expression left, Expression right, TokenType @operator, Span location);
}


public abstract class BinaryLayoutExpression(Expression left, Expression right, TokenType @operator, Span location)
    : Expression(location)
{
    public Expression Left { get; set; } = left;
    public Expression Right { get; set; } = right;
    public TokenType Operator { get; } = @operator;
}
