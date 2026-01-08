using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;




public abstract class Statement(Span location)
{
    public Span Location { get; } = location;




    public abstract void Process(IStatementProcessor processor);
    public abstract T Process<T>(IStatementProcessor<T> processor);
}
