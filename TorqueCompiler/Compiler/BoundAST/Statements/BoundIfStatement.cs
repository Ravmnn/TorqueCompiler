using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Expressions;


namespace Torque.Compiler.BoundAST.Statements;




public class BoundIfStatement(IfStatement syntax, BoundExpression condition, BoundStatement thenStatement, BoundStatement? elseStatement)
    : BoundStatement(syntax)
{
    public new IfStatement Syntax => (base.Syntax as IfStatement)!;

    public BoundExpression Condition { get; set; } = condition;
    public BoundStatement ThenStatement { get; } = thenStatement;
    public BoundStatement? ElseStatement { get; } = elseStatement;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessIf(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessIf(this);
}
