using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;




// statements that have members that are statements as well should have them as settable,
// so the desugarizer can modify them


public abstract class Statement(Span location)
{
    public Span Location { get; } = location;




    public abstract void Process(IStatementProcessor processor);
    public abstract T Process<T>(IStatementProcessor<T> processor);
}
