using Torque.Compiler.Tokens;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.AST.Statements;




public class ReturnStatement(Span location, Expression? expression = null) : Statement(location)
{
    public Expression? Expression { get; } = expression;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessReturn(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessReturn(this);
}
