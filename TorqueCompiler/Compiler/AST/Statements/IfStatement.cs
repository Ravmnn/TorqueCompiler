using Torque.Compiler.Tokens;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.AST.Statements;




public class IfStatement(Expression condition, Statement thenStatement, Statement? elseStatement, Span location) : Statement(location)
{
    public Expression Condition { get; } = condition;
    public Statement ThenStatement { get; } = thenStatement;
    public Statement? ElseStatement { get; } = elseStatement;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessIf(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessIf(this);
}
