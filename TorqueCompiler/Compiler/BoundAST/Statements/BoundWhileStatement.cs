using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Expressions;


namespace Torque.Compiler.BoundAST.Statements;




public class BoundWhileStatement(Statement syntax, BoundExpression condition, BoundStatement loop, BoundStatement? postLoop)
    : BoundStatement(syntax)
{
    public new WhileStatement Syntax => (base.Syntax as WhileStatement)!;

    public BoundExpression Condition { get; set; } = condition;
    public BoundStatement Loop { get; } = loop;
    public BoundStatement? PostLoop { get; } = postLoop;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessWhile(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessWhile(this);
}
