using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class PreFixExpression(Expression expression, TokenType @operator, Span location)
    : UnaryLayoutExpression(expression, @operator, location), IUnaryLayoutExpressionFactory
{
    public Expression Expression => Right;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessPreFix(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessPreFix(this);




    public static UnaryLayoutExpression Create(Expression right, TokenType @operator, Span location)
        => new PreFixExpression(right, @operator, location);
}
