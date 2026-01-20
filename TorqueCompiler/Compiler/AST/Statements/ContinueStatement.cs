using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;




public class ContinueStatement(Span location) : Statement(location)
{
    public override void Process(IStatementProcessor processor)
        => processor.ProcessContinue(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessContinue(this);
}
