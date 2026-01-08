using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public abstract class Expression(Span location)
{
    public Span Location { get; } = location;




    public abstract void Process(IExpressionProcessor processor);
    public abstract T Process<T>(IExpressionProcessor<T> processor);
}
