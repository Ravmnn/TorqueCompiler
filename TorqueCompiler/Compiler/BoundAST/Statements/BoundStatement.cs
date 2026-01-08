using Torque.Compiler.Tokens;
using Torque.Compiler.AST.Statements;


namespace Torque.Compiler.BoundAST.Statements;





public abstract class BoundStatement(Statement syntax)
{
    public Statement Syntax { get; } = syntax;
    public Span Location => Syntax.Location;




    public abstract void Process(IBoundStatementProcessor processor);
    public abstract T Process<T>(IBoundStatementProcessor<T> processor);
}
