using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class GroupingExpression(Expression expression, Span location) : Expression(location)
{
    public Expression Expression { get; } = expression;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessGrouping(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessGrouping(this);
}
