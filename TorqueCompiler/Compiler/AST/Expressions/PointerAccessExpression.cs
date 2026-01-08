using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class PointerAccessExpression(Expression pointer, Span location)
    : UnaryLayoutExpression(pointer, TokenType.Star, location), IUnaryLayoutExpressionFactory
{
    public Expression Pointer => Right;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessPointerAccess(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessPointerAccess(this);


    public static UnaryLayoutExpression Create(Expression right, TokenType @operator, Span location)
        => new PointerAccessExpression(right, location);
}
