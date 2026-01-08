using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class LogicExpression(Expression left, Expression right, TokenType @operator, Span location)
    : BinaryLayoutExpression(left, right, @operator, location), IBinaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessLogic(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessLogic(this);


    public static BinaryLayoutExpression Create(Expression left, Expression right, TokenType @operator, Span location)
        => new LogicExpression(left, right, @operator, location);
}
