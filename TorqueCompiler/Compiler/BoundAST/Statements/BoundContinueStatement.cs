using Torque.Compiler.AST.Statements;


namespace Torque.Compiler.BoundAST.Statements;




public class BoundContinueStatement(Statement syntax) : BoundStatement(syntax)
{
    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessContinue(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessContinue(this);
}
