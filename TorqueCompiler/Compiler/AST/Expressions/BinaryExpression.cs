using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class BinaryExpression(Expression left, Expression right, TokenType @operator, Span location)
    : BinaryLayoutExpression(left, right, @operator, location), IBinaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessBinary(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessBinary(this);




    public static BinaryLayoutExpression Create(Expression left, Expression right, TokenType @operator, Span location)
        => new BinaryExpression(left, right, @operator, location);
}
