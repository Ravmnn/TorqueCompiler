using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Expressions;


namespace Torque.Compiler.BoundAST.Statements;




public class BoundReturnStatement(ReturnStatement syntax, BoundExpression? expression) : BoundStatement(syntax)
{
    public new ReturnStatement Syntax => (base.Syntax as ReturnStatement)!;

    public BoundExpression? Expression { get; set; } = expression;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessReturn(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessReturn(this);
}
