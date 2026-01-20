using Torque.Compiler.AST.Statements;


namespace Torque.Compiler.BoundAST.Statements;




public class BoundBreakStatement(Statement syntax) : BoundStatement(syntax)
{
    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessBreak(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessBreak(this);
}
