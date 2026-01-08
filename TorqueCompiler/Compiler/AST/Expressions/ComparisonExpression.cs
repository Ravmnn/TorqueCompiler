using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class ComparisonExpression(Expression left, Expression right, TokenType @operator, Span location)
    : BinaryLayoutExpression(left, right, @operator, location), IBinaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessComparison(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessComparison(this);


    public static BinaryLayoutExpression Create(Expression left, Expression right, TokenType @operator, Span location)
        => new ComparisonExpression(left, right, @operator, location);
}
