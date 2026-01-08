using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.AST.Statements;




public class ExpressionStatement(Expression expression) : Statement(expression.Location)
{
    public Expression Expression { get; } = expression;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessExpression(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessExpression(this);
}
