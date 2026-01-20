using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;




public class BreakStatement(Span location) : Statement(location)
{
    public override void Process(IStatementProcessor processor)
        => processor.ProcessBreak(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessBreak(this);
}
