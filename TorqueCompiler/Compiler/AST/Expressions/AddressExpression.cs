using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class AddressExpression(Expression expression, Span location)
    : UnaryLayoutExpression(expression, TokenType.Ampersand, location), IUnaryLayoutExpressionFactory
{
    public Expression Expression => Right;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessAddress(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessAddress(this);


    public static UnaryLayoutExpression Create(Expression right, TokenType @operator, Span location)
        => new AddressExpression(right, location);
}
