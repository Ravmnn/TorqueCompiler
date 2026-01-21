using Torque.Compiler.Tokens;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.AST.Statements;




public class WhileStatement(Expression condition, Statement loop, Statement? postLoop, Span location) : Statement(location)
{
    public Expression Condition { get; set; } = condition;
    public Statement Loop { get; set; } = loop;
    public Statement? PostLoop { get; set; } = postLoop;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessWhile(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessWhile(this);
}
