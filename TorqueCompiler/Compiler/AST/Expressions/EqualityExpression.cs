using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class EqualityExpression(Expression left, Expression right, TokenType @operator, Span location)
    : BinaryLayoutExpression(left, right, @operator, location), IBinaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessEquality(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessEquality(this);


    public static BinaryLayoutExpression Create(Expression left, Expression right, TokenType @operator, Span location)
        => new EqualityExpression(left, right, @operator, location);
}
