using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class UnaryExpression(Expression right, TokenType @operator, Span location)
    : UnaryLayoutExpression(right, @operator, location), IUnaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessUnary(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessUnary(this);


    public static UnaryLayoutExpression Create(Expression right, TokenType @operator, Span location)
        => new UnaryExpression(right, @operator, location);
}
