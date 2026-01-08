using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Expressions;


namespace Torque.Compiler.BoundAST.Statements;




public class BoundExpressionStatement(ExpressionStatement syntax, BoundExpression expression) : BoundStatement(syntax)
{
    public new ExpressionStatement Syntax => (base.Syntax as ExpressionStatement)!;

    public BoundExpression Expression { get; } = expression;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessExpression(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessExpression(this);
}
